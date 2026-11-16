using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using DataLayer.Controllers;
using DataLayer.Infrastructure;
using DataLayer.Models;
using DataLayer.Models.Dto;
using PostOffice.Infrastructure;
using PostOffice.Views;

namespace PostOffice.ViewModels;

// Модель представления главного окна: данные для вкладок, текущий пользователь
// и команды по пунктам задания. Окно только показывает эти данные и вызывает команды;
// команда доступна, если она есть у роли пользователя. Приложение стартует гостем.
// Исполнители команд - в отдельных частях класса: вход, запросы и отчёты -
// MainWindowViewModel.Exec.cs, операции отделения и подписки - MainWindowViewModel.Operations.cs
public partial class MainWindowViewModel : INotifyPropertyChanged
{
    // контроллеры слоя данных
    private readonly QueriesController _queries = new(Connection.ConnectionString);
    private readonly OperationsController _operations = new(Connection.ConnectionString);
    private readonly ServiceController _service = new(Connection.ConnectionString);
    private readonly CatalogController _catalog = new();

    // роли пользователя, выполнившего вход; пустой список - гость
    private List<AccountRole> _roles = [];


    #region Пользователь и роли

    // пользователь и его роли для строки состояния
    private string _userName = "гость";
    public string UserName {
        get => _userName;
        set => SetField(ref _userName, value);
    } // UserName

    // вход не выполнен
    public bool IsGuest => _roles.Count == 0;

    // подписчик: свои подписки и их история; роль подписчика - это строка в Subscribers
    public bool IsSubscriber => _roles.Any(role => role.SubscriberId != null);

    // почтальон: свои участки с адресами
    public bool IsPostman => _roles.Any(role => role.Role == "Почтальон");

    // оператор или заведующий: клиенты, подписка, издания, запросы, справка, отчёт
    public bool IsOperator => _roles.Any(role => role.Role is "Оператор" or "Заведующий");

    // заведующий: приём и увольнение почтальонов
    public bool IsManager => _roles.Any(role => role.Role == "Заведующий");

    // вкладка участков: работникам отделения и почтальону
    public bool CanSeeDistricts => IsOperator || IsPostman;

    // номера пользователя в базе для его ролей: подписчик, почтальон,
    // работник, оформляющий подписку
    private int SubscriberId => _roles.First(role => role.SubscriberId != null).SubscriberId!.Value;
    private int PostmanId => _roles.First(role => role.Role == "Почтальон").EmployeeId!.Value;
    private int OperatorId => _roles.First(role => role.Role is "Оператор" or "Заведующий").EmployeeId!.Value;

    #endregion


    #region Данные для окна

    // номер открытой вкладки: 0 - издания, 1 - мои подписки, 2 - клиенты,
    // 3 - почтальоны и участки, 4 - запросы, 5 - справка
    private int _tbcSelectedIndex;
    public int TbcSelectedIndex {
        get => _tbcSelectedIndex;
        set => SetField(ref _tbcSelectedIndex, value);
    } // TbcSelectedIndex

    // вкладка внутри «Почтальонов и участков»: 0 - работающие почтальоны,
    // 1 - участки, 2 - дома участков почтальона
    private int _districtsTabIndex;
    public int DistrictsTabIndex {
        get => _districtsTabIndex;
        set => SetField(ref _districtsTabIndex, value);
    } // DistrictsTabIndex

    // сообщение о последнем выполненном действии
    private string _status = "Каталог подписных изданий. Для работы с подписками выполните вход или зарегистрируйтесь";
    public string Status {
        get => _status;
        set => SetField(ref _status, value);
    } // Status

    // каталог подписных изданий - виден всем, в том числе гостю
    private List<Publication> _publications = [];
    public List<Publication> Publications {
        get => _publications;
        set => SetField(ref _publications, value);
    } // Publications

    // подписки пользователя-подписчика и их состояние на сегодня
    private List<SubscriptionState> _mySubscriptions = [];
    public List<SubscriptionState> MySubscriptions {
        get => _mySubscriptions;
        set => SetField(ref _mySubscriptions, value);
    } // MySubscriptions

    // подписка, выбранная в таблице «Мои подписки» - для повторной выдачи квитанции
    private SubscriptionState? _selectedSubscription;
    public SubscriptionState? SelectedSubscription {
        get => _selectedSubscription;
        set => SetField(ref _selectedSubscription, value);
    } // SelectedSubscription

    // клиенты отделения с адресами
    private List<SubscriberAddress> _subscribers = [];
    public List<SubscriberAddress> Subscribers {
        get => _subscribers;
        set => SetField(ref _subscribers, value);
    } // Subscribers

    // клиент, выбранный в таблице клиентов: ему оформляется подписка, выдаётся логин,
    // меняется адрес
    private SubscriberAddress? _selectedSubscriber;
    public SubscriberAddress? SelectedSubscriber {
        get => _selectedSubscriber;
        set => SetField(ref _selectedSubscriber, value);
    } // SelectedSubscriber

    // работающие почтальоны и число их участков
    private List<PostmanLoad> _postmen = [];
    public List<PostmanLoad> Postmen {
        get => _postmen;
        set => SetField(ref _postmen, value);
    } // Postmen

    // участки и их почтальоны
    private List<District> _districts = [];
    public List<District> Districts {
        get => _districts;
        set => SetField(ref _districts, value);
    } // Districts

    // дома участков пользователя-почтальона
    private List<PostmanHouse> _myHouses = [];
    public List<PostmanHouse> MyHouses {
        get => _myHouses;
        set => SetField(ref _myHouses, value);
    } // MyHouses

    #endregion


    #region Результаты запросов и справка

    // запрос и его параметры над результатом
    private string _queryCaption = "Выберите запрос";
    public string QueryCaption {
        get => _queryCaption;
        set => SetField(ref _queryCaption, value);
    } // QueryCaption

    // номер вкладки с результатом запроса: 0 - запрос 1, ..., 5 - запрос 6
    private int _queryIndex;
    public int QueryIndex {
        get => _queryIndex;
        set => SetField(ref _queryIndex, value);
    } // QueryIndex

    private List<PublicationCopies> _query1Rows = [];
    public List<PublicationCopies> Query1Rows {
        get => _query1Rows;
        set => SetField(ref _query1Rows, value);
    } // Query1Rows

    private List<PostmanAddress> _query2Rows = [];
    public List<PostmanAddress> Query2Rows {
        get => _query2Rows;
        set => SetField(ref _query2Rows, value);
    } // Query2Rows

    private List<CitizenNewspaper> _query3Rows = [];
    public List<CitizenNewspaper> Query3Rows {
        get => _query3Rows;
        set => SetField(ref _query3Rows, value);
    } // Query3Rows

    // запрос 4 отвечает одним числом
    private string _query4Result = "";
    public string Query4Result {
        get => _query4Result;
        set => SetField(ref _query4Result, value);
    } // Query4Result

    private List<DistrictCopies> _query5Rows = [];
    public List<DistrictCopies> Query5Rows {
        get => _query5Rows;
        set => SetField(ref _query5Rows, value);
    } // Query5Rows

    private List<AverageTerm> _query6Rows = [];
    public List<AverageTerm> Query6Rows {
        get => _query6Rows;
        set => SetField(ref _query6Rows, value);
    } // Query6Rows

    // справка: период и три числа
    private string _summaryCaption = "Справка ещё не формировалась";
    public string SummaryCaption {
        get => _summaryCaption;
        set => SetField(ref _summaryCaption, value);
    } // SummaryCaption

    private int _summarySubscribers;
    public int SummarySubscribers {
        get => _summarySubscribers;
        set => SetField(ref _summarySubscribers, value);
    } // SummarySubscribers

    private int _summaryNewspapers;
    public int SummaryNewspapers {
        get => _summaryNewspapers;
        set => SetField(ref _summaryNewspapers, value);
    } // SummaryNewspapers

    private int _summaryMagazines;
    public int SummaryMagazines {
        get => _summaryMagazines;
        set => SetField(ref _summaryMagazines, value);
    } // SummaryMagazines

    #endregion


    #region Команды

    // вход по логину и паролю
    private RelayCommand _loginCommand;
    public RelayCommand LoginCommand => _loginCommand;

    // регистрация гостя подписчиком с учётной записью
    private RelayCommand _registerCommand;
    public RelayCommand RegisterCommand => _registerCommand;

    // смена пароля пользователем
    private RelayCommand _changePasswordCommand;
    public RelayCommand ChangePasswordCommand => _changePasswordCommand;

    // выход из учётной записи - снова гость
    private RelayCommand _logoutCommand;
    public RelayCommand LogoutCommand => _logoutCommand;

    // вернуть данные базы к исходным
    private RelayCommand _regenerateCommand;
    public RelayCommand RegenerateCommand => _regenerateCommand;

    // перечитать данные, изменённые в другом экземпляре приложения
    private RelayCommand _refreshCommand;
    public RelayCommand RefreshCommand => _refreshCommand;

    // принять почтальона на работу и выделить ему участок
    private RelayCommand _hirePostmanCommand;
    public RelayCommand HirePostmanCommand => _hirePostmanCommand;

    // уволить почтальона, распределив его участки
    private RelayCommand _firePostmanCommand;
    public RelayCommand FirePostmanCommand => _firePostmanCommand;

    // передать участок другому почтальону
    private RelayCommand _reassignDistrictCommand;
    public RelayCommand ReassignDistrictCommand => _reassignDistrictCommand;

    // зарегистрировать нового клиента
    private RelayCommand _newSubscriberCommand;
    public RelayCommand NewSubscriberCommand => _newSubscriberCommand;

    // выдать логин выбранному клиенту
    private RelayCommand _setAccountCommand;
    public RelayCommand SetAccountCommand => _setAccountCommand;

    // изменить адрес выбранного клиента
    private RelayCommand _changeAddressCommand;
    public RelayCommand ChangeAddressCommand => _changeAddressCommand;

    // оформить подписку и выдать квитанцию
    private RelayCommand _subscribeCommand;
    public RelayCommand SubscribeCommand => _subscribeCommand;

    // повторно выдать файл квитанции по выбранной подписке
    private RelayCommand _receiptCommand;
    public RelayCommand ReceiptCommand => _receiptCommand;

    // выдать квитанцию выбранному клиенту, в том числе без учётной записи
    private RelayCommand _clientReceiptCommand;
    public RelayCommand ClientReceiptCommand => _clientReceiptCommand;

    // добавить новое подписное издание
    private RelayCommand _newPublicationCommand;
    public RelayCommand NewPublicationCommand => _newPublicationCommand;

    // изменить цену издания
    private RelayCommand _changePriceCommand;
    public RelayCommand ChangePriceCommand => _changePriceCommand;

    // запрос 1: наименование и количество экземпляров всех изданий отделения
    private RelayCommand _query1Command;
    public RelayCommand Query1Command => _query1Command;

    // запрос 2: фамилия почтальона, обслуживающего подписчика по заданному адресу
    private RelayCommand _query2Command;
    public RelayCommand Query2Command => _query2Command;

    // запрос 3: газеты, которые выписывает гражданин с заданным ФИО
    private RelayCommand _query3Command;
    public RelayCommand Query3Command => _query3Command;

    // запрос 4: сколько почтальонов работает в отделении
    private RelayCommand _query4Command;
    public RelayCommand Query4Command => _query4Command;

    // запрос 5: участок с максимальным количеством экземпляров
    private RelayCommand _query5Command;
    public RelayCommand Query5Command => _query5Command;

    // запрос 6: средний срок подписки по каждому изданию
    private RelayCommand _query6Command;
    public RelayCommand Query6Command => _query6Command;

    // справка о подписчиках, газетах и журналах на текущий момент
    private RelayCommand _summaryCommand;
    public RelayCommand SummaryCommand => _summaryCommand;

    // отчёт о доставке газет и журналов по участкам
    private RelayCommand _reportCommand;
    public RelayCommand ReportCommand => _reportCommand;

    // показать сведения о программе
    private RelayCommand _aboutCommand = new(
        execute: obj => new AboutWindow { Owner = Application.Current.MainWindow }.ShowDialog());
    public RelayCommand AboutCommand => _aboutCommand;

    // завершить работу приложения
    private RelayCommand _exitCommand = new(
        execute: obj => Application.Current.Shutdown());
    public RelayCommand ExitCommand => _exitCommand;

    #endregion


    // стартовые данные - каталог изданий, пользователь - гость
    public MainWindowViewModel() {
        _loginCommand = new RelayCommand(execute: LoginExec, canExecute: obj => IsGuest);
        _registerCommand = new RelayCommand(execute: RegisterExec, canExecute: obj => IsGuest);
        _changePasswordCommand = new RelayCommand(execute: ChangePasswordExec, canExecute: obj => !IsGuest);
        _logoutCommand = new RelayCommand(execute: LogoutExec, canExecute: obj => !IsGuest);
        _regenerateCommand = new RelayCommand(execute: RegenerateExec);
        _refreshCommand = new RelayCommand(execute: RefreshExec);
        _hirePostmanCommand = new RelayCommand(execute: HirePostmanExec, canExecute: obj => IsManager);
        _firePostmanCommand = new RelayCommand(execute: FirePostmanExec, canExecute: obj => IsManager);
        _reassignDistrictCommand = new RelayCommand(execute: ReassignDistrictExec, canExecute: obj => IsManager);
        _newSubscriberCommand = new RelayCommand(execute: NewSubscriberExec, canExecute: obj => IsOperator);
        _setAccountCommand = new RelayCommand(execute: SetAccountExec,
            canExecute: obj => IsOperator && SelectedSubscriber != null);
        _changeAddressCommand = new RelayCommand(execute: ChangeAddressExec,
            canExecute: obj => IsOperator && SelectedSubscriber != null);
        _subscribeCommand = new RelayCommand(execute: SubscribeExec, canExecute: obj => IsOperator);
        _receiptCommand = new RelayCommand(execute: ReceiptExec,
            canExecute: obj => IsSubscriber && SelectedSubscription != null);
        _clientReceiptCommand = new RelayCommand(execute: ClientReceiptExec,
            canExecute: obj => IsOperator && SelectedSubscriber != null);
        _newPublicationCommand = new RelayCommand(execute: NewPublicationExec, canExecute: obj => IsOperator);
        _changePriceCommand = new RelayCommand(execute: ChangePriceExec, canExecute: obj => IsOperator);
        _query1Command = new RelayCommand(execute: Query1Exec, canExecute: obj => IsOperator);
        _query2Command = new RelayCommand(execute: Query2Exec, canExecute: obj => IsOperator);
        _query3Command = new RelayCommand(execute: Query3Exec, canExecute: obj => IsOperator);
        _query4Command = new RelayCommand(execute: Query4Exec, canExecute: obj => IsOperator);
        _query5Command = new RelayCommand(execute: Query5Exec, canExecute: obj => IsOperator);
        _query6Command = new RelayCommand(execute: Query6Exec, canExecute: obj => IsOperator);
        _summaryCommand = new RelayCommand(execute: SummaryExec, canExecute: obj => IsOperator);
        _reportCommand = new RelayCommand(execute: ReportExec, canExecute: obj => IsOperator);

        Publications = _catalog.Publications();
    } // MainWindowViewModel


    #region Реализация интерфейса INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string prop = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));


    // запись значения в поле и оповещение об изменении свойства
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return false;
        } // if

        field = value;
        OnPropertyChanged(propertyName!);
        return true;
    } // SetField

    #endregion

} // class MainWindowViewModel
