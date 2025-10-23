using SmartHome2.ViewModels;

namespace SmartHome2.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly LoginVm _vm;

        public LoginPage(LoginVm vm)
        {
            InitializeComponent();
            BindingContext = vm;
            _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Check server status and auto-login if guest mode available
            await _vm.InitializeAsync();
        }
    }
}