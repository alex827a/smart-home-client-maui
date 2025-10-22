using SmartHome2.ViewModels;

namespace SmartHome2.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsVm vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}