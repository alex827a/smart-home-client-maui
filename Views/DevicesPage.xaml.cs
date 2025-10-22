using SmartHome2.ViewModels;

namespace SmartHome2.Views
{
    public partial class DevicesPage : ContentPage
    {
        private readonly DevicesVm _vm;

        public DevicesPage(DevicesVm vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            System.Diagnostics.Debug.WriteLine("DevicesPage OnAppearing - refreshing permissions");
            System.Diagnostics.Debug.WriteLine($"DevicesPage: CurrentUserRole={Services.AppSettings.CurrentUserRole}, IsAdmin={Services.AppSettings.IsAdmin}");
            
            // Refresh permissions when page appears
            _vm.RefreshPermissions();
        }
    }
}