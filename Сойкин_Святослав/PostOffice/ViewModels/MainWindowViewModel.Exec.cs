using System.IO;
using System.Windows;
using System.Windows.Input;
using DataLayer.Models.Dto;
using PostOffice.Documents;
using PostOffice.Infrastructure;
using PostOffice.Models;
using PostOffice.Views;
using QuestPDF.Fluent;

namespace PostOffice.ViewModels;

// Исполнители команд главного окна: вход и учётная запись, служебные данные,
// запросы, справка, отчёт о доставке. Запросы «на текущий момент» выполняются
// на сегодняшнюю дату. Операции отделения и подписки - в MainWindowViewModel.Operations.cs
public partial class MainWindowViewModel
{
    // сегодняшняя дата для запросов «на текущий момент»
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // папка файлов, которые выдаёт программа: квитанции и отчёты
    private static readonly string FilesFolder = Path.Combine(AppContext.BaseDirectory, "App_Files");


    #region Вход, регистрация, пароль, выход

    private void LoginExec(object obj) {
        var loginWindow = new LoginWindow { Owner = Application.Current.MainWindow };
        if (loginWindow.ShowDialog() != true) {
            return;
        } // if

        var (roles, message) = _operations.CheckLogin(loginWindow.Login, loginWindow.Password);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        SignIn(roles);
    } // LoginExec


    // гость регистрируется подписчиком и сразу входит под своей учётной записью
    private void RegisterExec(object obj) {
        var card = new ClientCard();
        var streets = _catalog.Streets();

        // после отказа окно открывается снова с тем, что уже введено
        string? message;
        do {
            var clientWindow = new ClientWindow("Регистрация подписчика", card, streets) {
                Owner = Application.Current.MainWindow
            };
            if (clientWindow.ShowDialog() != true) {
                return;
            } // if

            message = _operations.RegisterSubscriber(card.Passport, card.Surname, card.Name, card.Patronymic,
                card.HouseId!.Value, card.Flat, card.Login, card.Password).Message;
            if (message != null) {
                Utils.Fail(message);
            } // if
        } while (message != null);

        var (roles, loginMessage) = _operations.CheckLogin(card.Login!, card.Password!);
        if (loginMessage != null) {
            Utils.Fail(loginMessage);
            return;
        } // if

        SignIn(roles);
        Utils.Info($"Регистрация выполнена, вы вошли как {UserName}. Подписку оформляет оператор " +
                   "отделения, а ваши подписки и их история видны на вкладке «Мои подписки»");
    } // RegisterExec


    private void ChangePasswordExec(object obj) {
        var passwordWindow = new PasswordWindow { Owner = Application.Current.MainWindow };
        if (passwordWindow.ShowDialog() != true) {
            return;
        } // if

        var message = _operations.ChangePassword(_roles[0].Login, passwordWindow.OldPassword,
            passwordWindow.NewPassword);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        Status = "Пароль изменён";
        Utils.Info("Пароль изменён. При следующем входе используйте новый пароль");
    } // ChangePasswordExec


    private void LogoutExec(object obj) {
        SetRoles([]);
        UserName = "гость";

        MySubscriptions = [];
        Subscribers = [];
        Postmen = [];
        Districts = [];
        MyHouses = [];

        TbcSelectedIndex = 0;
        Status = "Выполнен выход из учётной записи";
    } // LogoutExec


    // вход с ролями учётной записи: данные вкладок и вкладка главной роли -
    // оператора, затем почтальона, затем подписчика
    private void SignIn(List<AccountRole> roles) {
        SetRoles(roles);
        UserName = $"{roles[0].ShortName} ({string.Join(", ", roles.Select(role => role.Role))})";
        LoadUserData();

        if (IsOperator) {
            TbcSelectedIndex = 2;
            DistrictsTabIndex = 0;
        } else if (IsPostman) {
            TbcSelectedIndex = 3;
            DistrictsTabIndex = 2;
        } else {
            TbcSelectedIndex = 1;
        } // if

        Status = $"Выполнен вход: {UserName}";
    } // SignIn


    // роли пользователя меняют доступность вкладок и команд
    private void SetRoles(List<AccountRole> roles) {
        _roles = roles;

        OnPropertyChanged(nameof(IsGuest));
        OnPropertyChanged(nameof(IsSubscriber));
        OnPropertyChanged(nameof(IsPostman));
        OnPropertyChanged(nameof(IsOperator));
        OnPropertyChanged(nameof(IsManager));
        OnPropertyChanged(nameof(CanSeeDistricts));
        CommandManager.InvalidateRequerySuggested();
    } // SetRoles


    // данные вкладок, доступных ролям пользователя; вызывается при входе
    // и после каждой операции, изменившей данные
    private void LoadUserData() {
        Publications = _catalog.Publications();

        if (IsSubscriber) {
            MySubscriptions = _queries.SubscriberHistory(SubscriberId, Today);
        } // if

        if (IsPostman) {
            MyHouses = _queries.PostmanHouses(PostmanId);
        } // if

        if (IsOperator) {
            Subscribers = _queries.Subscribers();
            Postmen = _queries.WorkingPostmen();
            Districts = _queries.Districts();
        } // if
    } // LoadUserData


    private void RefreshExec(object obj) {
        LoadUserData();
        Status = "Данные перечитаны из базы";
    } // RefreshExec

    #endregion


    #region Вернуть исходные данные

    private void RegenerateExec(object obj) {
        if (!Utils.Confirm("Вернуть данные базы к исходным? Подписки, клиенты и работники, " +
                           "добавленные после заполнения, будут удалены.")) {
            return;
        } // if

        // скрипты заполнения и триггеров копируются в папку Scripts рядом с приложением
        _service.ResetData(Path.Combine(AppContext.BaseDirectory, "Scripts"));

        LogoutExec(obj);
        Publications = _catalog.Publications();
        Status = "Данные базы возвращены к исходным. Выполните вход заново";
    } // RegenerateExec

    #endregion


    #region Запрос 1. Экземпляры всех изданий

    private void Query1Exec(object obj) {
        Query1Rows = _queries.PublicationsCopies(Today);

        QueryCaption = "Запрос 1. Наименование и количество экземпляров всех изданий, " +
                       $"получаемых отделением, на {Today:dd.MM.yyyy}";
        ShowQuery(0, $"Запрос 1 выполнен, изданий: {Query1Rows.Count}");
    } // Query1Exec

    #endregion


    #region Запрос 2. Почтальон по адресу

    private void Query2Exec(object obj) {
        var inputWindow = new InputWindow("Запрос 2. Почтальон по адресу",
            new InputField("Адрес подписчика", _queries.Houses(), "Address")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var house = (HouseAddress)inputWindow.Values[0];
        Query2Rows = _queries.PostmanByAddress(house.Street, house.Building);

        QueryCaption = $"Запрос 2. Почтальон, обслуживающий адрес {house.Address}";
        ShowQuery(1, $"Запрос 2 выполнен: {house.Address}");
    } // Query2Exec

    #endregion


    #region Запрос 3. Газеты гражданина

    private void Query3Exec(object obj) {
        var inputWindow = new InputWindow("Запрос 3. Газеты гражданина",
            new InputField("Фамилия, имя, отчество", _queries.Citizens(), "FullName")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var citizen = (Citizen)inputWindow.Values[0];
        Query3Rows = _queries.CitizenNewspapers(citizen.Surname, citizen.Name, citizen.Patronymic, Today);

        QueryCaption = $"Запрос 3. Газеты, которые выписывает {citizen.FullName}, на {Today:dd.MM.yyyy}";
        ShowQuery(2, $"Запрос 3 выполнен, газет: {Query3Rows.Count}");
    } // Query3Exec

    #endregion


    #region Запрос 4. Число работающих почтальонов

    private void Query4Exec(object obj) {
        Query4Result = $"{_queries.WorkingPostmenCount()}";

        QueryCaption = "Запрос 4. Сколько почтальонов работает в почтовом отделении";
        ShowQuery(3, $"Запрос 4 выполнен, работает почтальонов: {Query4Result}");
    } // Query4Exec

    #endregion


    #region Запрос 5. Участок с максимумом экземпляров

    private void Query5Exec(object obj) {
        Query5Rows = _queries.MaxCopiesDistricts(Today);

        QueryCaption = $"Запрос 5. Участок с максимальным количеством экземпляров на {Today:dd.MM.yyyy}" +
                       " (при равенстве выводятся все такие участки)";
        ShowQuery(4, $"Запрос 5 выполнен, участков: {Query5Rows.Count}");
    } // Query5Exec

    #endregion


    #region Запрос 6. Средний срок подписки по изданиям

    private void Query6Exec(object obj) {
        Query6Rows = _queries.AverageTermByPublication();

        QueryCaption = "Запрос 6. Средний срок подписки по каждому изданию за всё время, месяцев";
        ShowQuery(5, $"Запрос 6 выполнен, изданий: {Query6Rows.Count}");
    } // Query6Exec


    // открыть вкладку запросов на результате запроса
    private void ShowQuery(int queryIndex, string status) {
        QueryIndex = queryIndex;
        TbcSelectedIndex = 4;
        Status = status;
    } // ShowQuery

    #endregion


    #region Справка

    private void SummaryExec(object obj) {
        var summary = _queries.SubscriptionsSummary(Today.Year);

        SummaryCaption = $"Справка на {Today:dd.MM.yyyy}: подписки, оформленные в {Today.Year} году";
        SummarySubscribers = summary.Subscribers;
        SummaryNewspapers = summary.Newspapers;
        SummaryMagazines = summary.Magazines;

        TbcSelectedIndex = 5;
        Status = "Справка сформирована";
    } // SummaryExec

    #endregion


    #region Отчёт о доставке

    // отчёт строится на сегодня и сохраняется файлом PDF; в имени файла - дата отчёта
    private void ReportExec(object obj) {
        var folder = Path.Combine(FilesFolder, "Reports");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Delivery_{Today:yyyy-MM-dd}.pdf");

        var report = new DeliveryReportDocument(Today, _queries.DeliveryLines(Today), _queries.DeliveryTotals(Today));
        try {
            report.GeneratePdf(path);
        } catch (IOException) {
            Utils.Fail($"Не удалось записать файл {path}: он открыт в другой программе. Закройте его и повторите");
            return;
        } // try

        Status = $"Отчёт о доставке сохранён: {Path.GetFileName(path)}";
        if (Utils.Confirm($"Отчёт о доставке на {Today:dd.MM.yyyy} сохранён в файл\n{path}\n\nОткрыть его?")) {
            Utils.OpenFile(path);
        } // if
    } // ReportExec

    #endregion

} // class MainWindowViewModel
