using System.Globalization;
using System.Windows;
using DataLayer.Infrastructure;
using PostOffice.Infrastructure;

namespace PostOffice;

public partial class App : Application
{

    // Русская запись чисел, дат и денег для всего приложения
    protected override void OnStartup(StartupEventArgs e) {
        // Культура задаётся до базового метода: он открывает главное окно
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("ru-RU");

        // квитанции и отчёт строит библиотека QuestPDF: бесплатная лицензия для учебной работы
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        base.OnStartup(e);
    } // OnStartup


    // Единое место обработки ошибок приложения: ошибка команды показывается
    // сообщением, и приложение продолжает работу. Для ошибки базы показывается
    // фраза триггера, а не системный текст
    public App() {
        DispatcherUnhandledException += (sender, e) => {
            Utils.Fail(DbErrors.Text(e.Exception));
            e.Handled = true;

            // ошибка при создании главного окна (например, база недоступна):
            // окна нет, работать не с чем - приложение завершается
            if (MainWindow == null || !MainWindow.IsLoaded) {
                Shutdown();
            } // if
        };
    } // App

} // class App
