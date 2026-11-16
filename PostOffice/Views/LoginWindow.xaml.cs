using System.Windows;
using PostOffice.Infrastructure;

namespace PostOffice.Views;

// Окно входа: собирает логин и пароль, проверку выполняет модель представления
public partial class LoginWindow : Window
{
    // введённые логин и пароль
    public string Login { get; private set; } = "";
    public string Password { get; private set; } = "";


    public LoginWindow() {
        InitializeComponent();
    } // LoginWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        if (TbxLogin.Text.Length == 0 || PbxPassword.Password.Length == 0) {
            Utils.Fail("Введите логин и пароль");
            return;
        } // if

        Login = TbxLogin.Text;
        Password = PbxPassword.Password;
        DialogResult = true;
    } // BtnOk_Click

} // class LoginWindow
