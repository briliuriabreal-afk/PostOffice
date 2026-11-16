using System.Windows;
using System.Windows.Input;

namespace PostOffice.Views;

public partial class AboutWindow : Window
{

    public AboutWindow() {
        InitializeComponent();
    } // AboutWindow


    private void AboutWindow_KeyDown(object sender, KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            Close();
        } // if
    } // AboutWindow_KeyDown

} // class AboutWindow
