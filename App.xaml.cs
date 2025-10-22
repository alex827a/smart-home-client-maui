using SmartHome2.Views;
using SmartHome2.Resources.Strings;

namespace SmartHome2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Load saved language
            AppResources.Instance.CurrentLanguage = Services.AppSettings.Language;

            MainPage = new AppShell();
        }
    }
}
