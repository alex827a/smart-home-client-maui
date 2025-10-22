using SQLite;
using SmartHome2.Models;

namespace SmartHome2.Services
{
    public class SqliteDataStore : IDataStore, IAsyncDisposable
    {
        readonly SQLiteAsyncConnection _db;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);
        private bool _initialized = false;
        private bool _disposed = false;

        class MetricsRow 
        { 
            [PrimaryKey] 
            public int Id { get; set; } = 1; 
            public double Temp { get; set; } 
            public int Humidity { get; set; } 
            public int Power { get; set; } 
            public string Ts { get; set; } = "";
        }

        class MetricsHistoryRow
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            public double Temp { get; set; }
            public int Humidity { get; set; }
            public int Power { get; set; }
            public string Ts { get; set; } = "";
            public DateTime RecordedAt { get; set; }
        }

        class DeviceRow  
        { 
            [PrimaryKey] 
            public string Id { get; set; } = ""; 
            public string Name { get; set; } = ""; 
            public bool IsOn { get; set; } 
            public string LastSeen { get; set; } = "";
        }

        public SqliteDataStore(string path) 
        {
            _db = new SQLiteAsyncConnection(path);
            System.Diagnostics.Debug.WriteLine($"SqliteDataStore: Created with path {path}");
        }

        private async Task EnsureInitializedAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SqliteDataStore));

            if (_initialized) 
                return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_initialized)
                {
                    System.Diagnostics.Debug.WriteLine("SqliteDataStore: Initializing tables...");
                    await _db.CreateTableAsync<MetricsRow>().ConfigureAwait(false);
                    await _db.CreateTableAsync<MetricsHistoryRow>().ConfigureAwait(false);
                    await _db.CreateTableAsync<DeviceRow>().ConfigureAwait(false);
                    _initialized = true;
                    System.Diagnostics.Debug.WriteLine("SqliteDataStore: Tables initialized");
                }
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task SaveMetricsAsync(MetricsDto m)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _db.InsertOrReplaceAsync(new MetricsRow 
            { 
                Temp = m.Temp, 
                Humidity = m.Humidity, 
                Power = m.Power, 
                Ts = m.Ts 
            }).ConfigureAwait(false);
        }

        public async Task<MetricsDto?> LoadMetricsAsync() 
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var r = await _db.FindAsync<MetricsRow>(1).ConfigureAwait(false);
            return r is null ? null : new(r.Temp, r.Humidity, r.Power, r.Ts);
        }

        public async Task SaveDevicesAsync(IEnumerable<DeviceDto> list) 
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _db.DeleteAllAsync<DeviceRow>().ConfigureAwait(false);
            await _db.InsertAllAsync(list.Select(d => new DeviceRow 
            { 
                Id = d.Id, 
                Name = d.Name, 
                IsOn = d.IsOn, 
                LastSeen = d.LastSeen 
            })).ConfigureAwait(false);
        }

        public async Task<List<DeviceDto>> LoadDevicesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var rows = await _db.Table<DeviceRow>().ToListAsync().ConfigureAwait(false);
            return rows.Select(r => new DeviceDto(r.Id, r.Name, r.IsOn, r.LastSeen)).ToList();
        }

        // Metric history
        public async Task SaveMetricHistoryAsync(MetricsDto m)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _db.InsertAsync(new MetricsHistoryRow
            {
                Temp = m.Temp,
                Humidity = m.Humidity,
                Power = m.Power,
                Ts = m.Ts,
                RecordedAt = DateTime.UtcNow
            }).ConfigureAwait(false);
            
            // Auto cleanup - keep only last 50 records (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await ClearOldMetricsAsync(50).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Background cleanup failed: {ex.Message}");
                }
            });
        }

        public async Task<List<MetricsDto>> LoadMetricHistoryAsync(int maxCount = 50)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            var rows = await _db.Table<MetricsHistoryRow>()
                .OrderByDescending(r => r.RecordedAt)
                .Take(maxCount)
                .ToListAsync()
                .ConfigureAwait(false);
            
            return rows
                .OrderBy(r => r.RecordedAt)
                .Select(r => new MetricsDto(r.Temp, r.Humidity, r.Power, r.Ts))
                .ToList();
        }

        public async Task ClearOldMetricsAsync(int keepCount = 50)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            
            await _cleanupLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var count = await _db.Table<MetricsHistoryRow>().CountAsync().ConfigureAwait(false);
                if (count <= keepCount)
                    return;

                var toDelete = count - keepCount;
                
                System.Diagnostics.Debug.WriteLine($"SqliteDataStore: Cleaning up {toDelete} old metrics...");
                
                // Single query instead of foreach
                await _db.ExecuteAsync(
                    @"DELETE FROM MetricsHistoryRow 
                      WHERE Id IN (
                          SELECT Id FROM MetricsHistoryRow 
                          ORDER BY RecordedAt ASC 
                          LIMIT ?
                      )", toDelete).ConfigureAwait(false);
                
                System.Diagnostics.Debug.WriteLine("SqliteDataStore: Cleanup completed");
            }
            finally
            {
                _cleanupLock.Release();
            }
        }

        public async Task ClearDevicesAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _db.DeleteAllAsync<DeviceRow>().ConfigureAwait(false);
        }

        public async Task ClearMetricHistoryAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _db.DeleteAllAsync<MetricsHistoryRow>().ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            System.Diagnostics.Debug.WriteLine("SqliteDataStore: Disposing...");

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _disposed = true;
                await _db.CloseAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine("SqliteDataStore: Database closed");
            }
            finally
            {
                _initLock.Release();
            }

            _initLock.Dispose();
            _cleanupLock.Dispose();

            System.Diagnostics.Debug.WriteLine("SqliteDataStore: Disposed");
        }
    }
}