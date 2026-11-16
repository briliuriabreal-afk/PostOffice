using System.Windows;
using PostOffice.ViewModels;

namespace PostOffice.Views;

// Представление паттерна MVVM: окно только показывает данные модели
// представления и вызывает её команды, собственных действий у него нет
public partial class MainWindow : Window
{

    // модель представления, к которой привязано окно
    private readonly MainWindowViewModel _mainWindowViewModel;


    public MainWindow() {
        InitializeComponent();

        // связь представления с моделью представления
        _mainWindowViewModel = new MainWindowViewModel();
        DataContext = _mainWindowViewModel;
    } // MainWindow

} // class MainWindow
