using System.IO;
using System.Windows;
using PostOffice.Infrastructure;
using PostOffice.ViewModels;

namespace PostOffice.Views;

// Окно оформления подписки: данные и команды заказа - в его модели представления,
// окно только проверяет готовность заказа при нажатии «Оформить».
// Оформляет подписки модель представления главного окна после закрытия этого окна
public partial class SubscriptionWindow : Window
{
    private readonly SubscriptionWindowViewModel _viewModel;


    public SubscriptionWindow(SubscriptionWindowViewModel viewModel) {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
    } // SubscriptionWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        try {
            _viewModel.Complete();
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        DialogResult = true;
    } // BtnOk_Click

} // class SubscriptionWindow
