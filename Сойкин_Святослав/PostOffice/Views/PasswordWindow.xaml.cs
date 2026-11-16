using System.IO;
using System.Windows;
using PostOffice.Infrastructure;
using PostOffice.Models;

namespace PostOffice.Views;

// Окно смены пароля: собирает текущий и новый пароль, смену выполняет модель
// представления. Требования к новому паролю - общие для всех паролей, из анкеты человека
public partial class PasswordWindow : Window
{
    // введённые пароли
    public string OldPassword { get; private set; } = "";
    public string NewPassword { get; private set; } = "";


    public PasswordWindow() {
        InitializeComponent();
        TblNew.Text = $"Новый пароль (не короче {PersonCard.MinPasswordLength} знаков)";
    } // PasswordWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        if (PbxOld.Password.Length == 0) {
            Utils.Fail("Введите текущий пароль");
            PbxOld.Focus();
            return;
        } // if

        if (PbxNew.Password != PbxRepeat.Password) {
            Utils.Fail("Новый пароль и его повтор не совпадают");
            PbxRepeat.Focus();
            return;
        } // if

        try {
            PersonCard.CheckPassword(PbxNew.Password);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            PbxNew.Focus();
            return;
        } // try

        OldPassword = PbxOld.Password;
        NewPassword = PbxNew.Password;
        DialogResult = true;
    } // BtnOk_Click

} // class PasswordWindow
