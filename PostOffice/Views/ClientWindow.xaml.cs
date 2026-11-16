using System.IO;
using System.Windows;
using System.Windows.Controls;
using DataLayer.Models;
using PostOffice.Infrastructure;
using PostOffice.Models;

namespace PostOffice.Views;

// Окно анкеты клиента: регистрация гостем и новый клиент у оператора.
// Привязки пишут прямо в анкету; пароль окно переносит само - у поля пароля
// нет привязки. Причину, по которой анкета значение не приняла, окно показывает
public partial class ClientWindow : Window
{
    private readonly ClientCard _card;

    // гость регистрируется, чтобы входить в программу, поэтому логин ему обязателен
    private readonly bool _loginRequired;


    public ClientWindow(string title, ClientCard card, List<Street> streets, bool loginRequired) {
        InitializeComponent();

        Title = title;
        _card = card;
        _loginRequired = loginRequired;
        DataContext = card;

        CbxStreet.ItemsSource = streets;
        if (!loginRequired) {
            TblLogin.Text = "Логин (можно не задавать и выдать позже)";
        } // if

        // после отказа окно открывается снова: сразу выбирается улица ранее выбранного
        // дома, сам дом выбирает привязка. Через другую улицу проходить нельзя - на ней
        // этого дома нет, и привязка записала бы в анкету «дом не выбран»
        if (card.HouseId == null) {
            CbxStreet.SelectedIndex = 0;
        } else {
            CbxStreet.SelectedItem = streets.First(street => street.Houses.Any(house => house.Id == card.HouseId));
        } // if

        if (card.Password != null) {
            PbxPassword.Password = card.Password;
        } // if
    } // ClientWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        // по Enter фокус из поля не уходит, поэтому введённое переносится в анкету явно
        foreach (var box in new[] { TbxPassport, TbxSurname, TbxName, TbxPatronymic, TbxFlat, TbxLogin }) {
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

} // class ClientWindow
