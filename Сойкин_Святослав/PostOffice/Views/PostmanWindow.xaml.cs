using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DataLayer.Models.Dto;
using PostOffice.Infrastructure;
using PostOffice.Models;

namespace PostOffice.Views;

// Окно анкеты принимаемого почтальона. Привязки пишут прямо в анкету; пароль окно
// переносит само - у поля пароля нет привязки. Причину, по которой анкета значение
// не приняла, окно показывает
public partial class PostmanWindow : Window
{
    private readonly PostmanCard _card;

    // логин и пароль нужны, если учётной записи у человека ещё нет
    private readonly bool _loginRequired;


    // person - человек с этим паспортом, который уже есть в базе: его ФИО подставлены
    // в анкету и не правятся; null - человек новый
    public PostmanWindow(PostmanCard card, List<District> districts, PersonInfo? person) {
        InitializeComponent();

        _card = card;
        _loginRequired = person == null;
        DataContext = card;
        CbxDistrict.ItemsSource = districts;

        if (person != null) {
            TbxSurname.IsReadOnly = true;
            TbxName.IsReadOnly = true;
            TbxPatronymic.IsReadOnly = true;
            FocusManager.SetFocusedElement(this, TbxLogin);
        } // if

        if (!_loginRequired) {
            TblLogin.Text = "У человека уже есть учётная запись: логин и пароль не нужны";
            TbxLogin.IsEnabled = false;
            PbxPassword.IsEnabled = false;
            FocusManager.SetFocusedElement(this, CbxDistrict);
        } // if

        if (card.Password != null) {
            PbxPassword.Password = card.Password;
        } // if
    } // PostmanWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        // по Enter фокус из поля не уходит, поэтому введённое переносится в анкету явно
        foreach (var box in new[] { TbxSurname, TbxName, TbxPatronymic, TbxLogin }) {
            box.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            if (Validation.GetHasError(box)) {
                Utils.Fail($"{Validation.GetErrors(box)[0].ErrorContent}");
                box.Focus();
                return;
            } // if
        } // foreach box

        try {
            _card.Password = PbxPassword.Password;
            _card.Check(_loginRequired);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        DialogResult = true;
    } // BtnOk_Click

} // class PostmanWindow
