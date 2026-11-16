using System.Diagnostics;
using System.Windows;

namespace PostOffice.Infrastructure;

// Сообщения пользователю: ошибка, сведения, подтверждение действия; открытие файла
public static class Utils
{
    // заголовок всех сообщений приложения
    private static readonly string Caption = "Почтовое отделение";


    // сообщение об ошибке; пока главное окно не создано - без окна-владельца
    public static void Fail(string message) {
        var owner = Application.Current.MainWindow;
        if (owner == null) {
            MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        } // if

        MessageBox.Show(owner, message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);
    } // Fail


    // сведения о выполненном действии
    public static void Info(string message) =>
        MessageBox.Show(Application.Current.MainWindow, message, Caption,
            MessageBoxButton.OK, MessageBoxImage.Information);


    // подтверждение действия, которое нельзя отменить
    public static bool Confirm(string message) =>
        MessageBox.Show(Application.Current.MainWindow, message, Caption,
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;


    // открыть файл программой, назначенной в системе для его типа
    public static void OpenFile(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

} // class Utils
