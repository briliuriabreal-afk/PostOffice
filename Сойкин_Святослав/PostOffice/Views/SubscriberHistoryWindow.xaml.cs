using System.Windows;
using DataLayer.Models.Dto;
using PostOffice.Infrastructure;

namespace PostOffice.Views;

public partial class SubscriberHistoryWindow : Window
{
    public SubscriptionState? SelectedSubscription => DgrSubscriptions.SelectedItem as SubscriptionState;


    public SubscriberHistoryWindow(string clientName, List<SubscriptionState> subscriptions) {
        InitializeComponent();
        Title = $"Подписки клиента {clientName}";
        DgrSubscriptions.ItemsSource = subscriptions;
    } // SubscriberHistoryWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        if (SelectedSubscription == null) {
            Utils.Fail("Выберите подписку");
            return;
        } // if

        DialogResult = true;
    } // BtnOk_Click

} // class SubscriberHistoryWindow
