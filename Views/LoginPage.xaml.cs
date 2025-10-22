using SmartHome2.ViewModels;

namespace SmartHome2.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage(LoginVm vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}