using System.IO;
using System.Windows;
using DataLayer.Infrastructure;
using DataLayer.Models;
using DataLayer.Models.Dto;
using PostOffice.Documents;
using PostOffice.Infrastructure;
using PostOffice.Models;
using PostOffice.Views;
using QuestPDF.Fluent;

namespace PostOffice.ViewModels;

// Исполнители операций отделения и подписки: приём, увольнение, передача участка,
// клиенты, подписка с квитанцией, каталог изданий. Причину отказа операция возвращает
// фразой: окно показывает её, и в базе ничего не меняется. Окна анкет после отказа
// открываются снова с уже введёнными значениями
public partial class MainWindowViewModel
{

    #region Принять почтальона на работу

    // сначала паспорт: человека, который уже есть в базе (например, подписчика),
    // анкета узнаёт - подставляет его ФИО и не просит логин, если учётная запись есть
    private void HirePostmanExec(object obj) {
        var inputWindow = new InputWindow("Приём почтальона на работу",
            new InputField("Серия и номер паспорта")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var card = new PostmanCard();
        try {
            card.Passport = (string)inputWindow.Values[0];
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        var person = _queries.PersonByPassport(card.Passport);
        if (person != null) {
            if (person.IsWorking) {
                Utils.Fail($"{person.Surname} {person.Name} уже работает в отделении");
                return;
            } // if

            card.Surname = person.Surname;
            card.Name = person.Name;
            card.Patronymic = person.Patronymic;
        } // if

        var districts = _queries.Districts();
        int? employeeId;
        string? message;
        do {
            var postmanWindow = new PostmanWindow(card, districts, person) {
                Owner = Application.Current.MainWindow
            };
            if (postmanWindow.ShowDialog() != true) {
                return;
            } // if

            (employeeId, message) = _operations.HirePostman(card.Passport, card.Surname, card.Name,
                card.Patronymic, card.Login, card.Password, card.DistrictId!.Value);
            if (message != null) {
                Utils.Fail(message);
            } // if
        } while (message != null);

        LoadUserData();
        TbcSelectedIndex = 3;
        DistrictsTabIndex = 1;

        var postman = Postmen.First(item => item.Id == employeeId);
        var district = districts.First(item => item.Id == card.DistrictId);
        Status = $"Принят почтальон {postman.ShortName}, ему передан участок № {district.Number}";
    } // HirePostmanExec

    #endregion


    #region Уволить почтальона

    private void FirePostmanExec(object obj) {
        var inputWindow = new InputWindow("Увольнение почтальона",
            new InputField("Почтальон", _queries.WorkingPostmen(), "ShortName")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var postman = (PostmanLoad)inputWindow.Values[0];
        if (!Utils.Confirm($"Уволить почтальона {postman.ShortName}? Его участки ({postman.DistrictCount}) " +
                           "перейдут к наименее загруженным почтальонам.")) {
            return;
        } // if

        var message = _operations.FirePostman(postman.Id);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        LoadUserData();
        TbcSelectedIndex = 3;
        DistrictsTabIndex = 1;
        Status = $"Почтальон {postman.ShortName} уволен, его участки переданы другим почтальонам";
    } // FirePostmanExec

    #endregion


    #region Передать участок

    // заведующий раздаёт участки вручную: например, перед увольнением
    // или второй участок новому почтальону
    private void ReassignDistrictExec(object obj) {
        TbcSelectedIndex = 3;
        DistrictsTabIndex = 1;

        var inputWindow = new InputWindow("Передача участка другому почтальону",
            new InputField("Номер участка", _queries.Districts(), "Number"),
            new InputField("Почтальон, которому передаётся участок", _queries.WorkingPostmen(), "ShortName")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var district = (District)inputWindow.Values[0];
        var postman = (PostmanLoad)inputWindow.Values[1];

        var message = _operations.ReassignDistrict(district.Id, postman.Id);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        LoadUserData();
        Status = $"Участок № {district.Number} передан почтальону {postman.ShortName}";
    } // ReassignDistrictExec

    #endregion


    #region Новый клиент

    // клиента у окошка регистрирует оператор; логин можно выдать сразу или позже
    private void NewSubscriberExec(object obj) {
        var card = new ClientCard();
        var streets = _catalog.Streets();

        int? subscriberId;
        string? message;
        do {
            var clientWindow = new ClientWindow("Новый клиент", card, streets, false) {
                Owner = Application.Current.MainWindow
            };
            if (clientWindow.ShowDialog() != true) {
                return;
            } // if

            (subscriberId, message) = _operations.RegisterSubscriber(card.Passport, card.Surname, card.Name,
                card.Patronymic, card.HouseId!.Value, card.Flat, card.Login, card.Password);
            if (message != null) {
                Utils.Fail(message);
            } // if
        } while (message != null);

        Subscribers = _queries.Subscribers();
        TbcSelectedIndex = 2;

        var client = Subscribers.First(item => item.Id == subscriberId);
        Status = $"Зарегистрирован клиент {client.ShortName}, {client.Address}";

        // клиент обычно приходит, чтобы сразу выписать издание
        if (Utils.Confirm($"Клиент {client.ShortName} зарегистрирован. Оформить ему подписку?")) {
            Subscribe(client);
        } // if
    } // NewSubscriberExec

    #endregion


    #region Выдать логин клиенту

    // клиенту без учётной записи; пароль временный, клиент сменит его после входа
    private void SetAccountExec(object obj) {
        var client = SelectedSubscriber!;
        var inputWindow = new InputWindow($"Учётная запись для клиента {client.ShortName}",
            new InputField($"Логин (от {PersonCard.MinLoginLength} знаков, без пробелов)"),
            new InputField($"Временный пароль (не короче {PersonCard.MinPasswordLength} знаков)")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var login = (string)inputWindow.Values[0];
        var password = (string)inputWindow.Values[1];
        try {
            PersonCard.CheckLogin(login);
            PersonCard.CheckPassword(password);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        var message = _operations.SetAccount(client.PersonId, login, password);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        Status = $"Клиенту {client.ShortName} выдан логин {login}";
        Utils.Info($"Клиенту {client.ShortName} выдан логин {login}. " +
                   "Пароль временный: после входа клиент сменит его командой «Сменить пароль»");
    } // SetAccountExec

    #endregion


    #region Изменить адрес клиента

    // подписки идут за клиентом, участок доставки меняется вместе с домом
    private void ChangeAddressExec(object obj) {
        var client = SelectedSubscriber!;
        var inputWindow = new InputWindow($"Новый адрес клиента {client.ShortName}",
            new InputField("Дом", _queries.Houses(), "Address"),
            new InputField("Квартира (для частного дома оставьте поле пустым)") { Optional = true }) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var house = (HouseAddress)inputWindow.Values[0];
        int? flat;
        try {
            flat = ClientCard.ParseFlat((string)inputWindow.Values[1]);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        var message = _catalog.ChangeAddress(client.Id, house.Id, flat);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        Subscribers = _queries.Subscribers();
        var moved = Subscribers.First(item => item.Id == client.Id);
        Status = $"Адрес клиента {client.ShortName} изменён: {moved.Address}, участок № {moved.DistrictNumber}";
    } // ChangeAddressExec

    #endregion


    #region Оформить подписку

    // подписка клиенту, выбранному в таблице клиентов; если не выбран - выбирается в окне
    private void SubscribeExec(object obj) => Subscribe(SelectedSubscriber);


    // заказ клиента: каждое издание - отдельная подписка со своей квитанцией,
    // отказ по одному изданию не отменяет остальные
    private void Subscribe(SubscriberAddress? client) {
        LoadUserData();
        var order = new SubscriptionWindowViewModel(Today, Subscribers, Publications) {
            Subscriber = Subscribers.FirstOrDefault(item => item.Id == client?.Id)
        };
        var subscriptionWindow = new SubscriptionWindow(order) {
            Owner = Application.Current.MainWindow
        };
        if (subscriptionWindow.ShowDialog() != true) {
            return;
        } // if

        var subscriber = order.Subscriber!;

        // итог по каждому изданию, файлы квитанций, число оформленных подписок
        List<string> results = [];
        List<string> receipts = [];
        var created = 0;
        foreach (var line in order.Lines) {
            // отказ процедуры приходит фразой, отказ триггера - исключением базы
            int? subscriptionId;
            string? message;
            try {
                (subscriptionId, message) = _operations.CreateSubscription(subscriber.Id, OperatorId,
                    line.Publication.Id, line.DateStart, line.Duration, line.Copies);
            } catch (Exception ex) {
                subscriptionId = null;
                message = DbErrors.Text(ex);
            } // try

            if (message != null) {
                results.Add($"{line.Publication.Title} - отказано: {message}");
                continue;
            } // if

            created++;

            // подписка уже оформлена: если файл не записался, квитанцию можно выдать повторно
            try {
                receipts.Add(SaveReceipt(subscriptionId!.Value));
                results.Add($"{line.Publication.Title} - оформлено, квитанция № {subscriptionId}");
            } catch (IOException) {
                results.Add($"{line.Publication.Title} - оформлено, квитанция № {subscriptionId}, но файл " +
                            "не записан. Повторная выдача: выберите клиента и нажмите «Квитанция клиенту»");
            } // try
        } // foreach line

        LoadUserData();
        TbcSelectedIndex = 2;
        Status = $"{subscriber.ShortName}: оформлено подписок {created} из {order.Lines.Count}";

        var text = string.Join("\n", results);
        if (receipts.Count == 0) {
            Utils.Fail(text);
            return;
        } // if

        if (Utils.Confirm($"{text}\n\nКвитанции сохранены в папку\n{Path.GetDirectoryName(receipts[0])}\n\n" +
                          "Открыть их?")) {
            foreach (var receipt in receipts) {
                Utils.OpenFile(receipt);
            } // foreach receipt
        } // if
    } // Subscribe


    // файл квитанции строится по данным, сохранённым в базе: повторная выдача
    // даёт тот же состав, срок и сумму
    private string SaveReceipt(int subscriptionId) {
        var folder = Path.Combine(FilesFolder, "Receipts");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Receipt_{subscriptionId}.pdf");

        var receipt = new ReceiptDocument(_queries.Receipt(subscriptionId));
        receipt.GeneratePdf(path);
        return path;
    } // SaveReceipt

    #endregion


    #region Квитанция по подписке

    // подписчик получает файл квитанции по любой своей подписке
    private void ReceiptExec(object obj) => IssueReceipt(SelectedSubscription!);


    private void ClientReceiptExec(object obj) {
        var client = SelectedSubscriber!;
        var subscriptions = _queries.SubscriberHistory(client.Id, Today);
        if (subscriptions.Count == 0) {
            Utils.Info($"У клиента {client.ShortName} нет оформленных подписок");
            return;
        } // if

        var historyWindow = new SubscriberHistoryWindow(client.ShortName, subscriptions) {
            Owner = Application.Current.MainWindow
        };
        if (historyWindow.ShowDialog() == true) {
            IssueReceipt(historyWindow.SelectedSubscription!);
        } // if
    } // ClientReceiptExec


    private void IssueReceipt(SubscriptionState subscription) {
        string path;
        try {
            path = SaveReceipt(subscription.Id);
        } catch (IOException) {
            Utils.Fail($"Не удалось записать файл квитанции № {subscription.Id}: он открыт в другой программе. " +
                       "Закройте его и повторите");
            return;
        } // try

        Status = $"Квитанция № {subscription.Id} сохранена: {Path.GetFileName(path)}";
        if (Utils.Confirm($"Квитанция № {subscription.Id} ({subscription.Title}) сохранена в файл\n{path}\n\n" +
                          "Открыть её?")) {
            Utils.OpenFile(path);
        } // if
    } // IssueReceipt

    #endregion


    #region Новое издание

    private void NewPublicationExec(object obj) {
        var inputWindow = new InputWindow("Новое подписное издание",
            new InputField("Подписной индекс (до 12 знаков)"),
            new InputField("Название (до 100 знаков)"),
            new InputField("Вид издания", _catalog.PublicationTypes(), "Name"),
            new InputField("Цена одного экземпляра за месяц, руб. (например, 120,50)")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var pubIndex = (string)inputWindow.Values[0];
        var title = (string)inputWindow.Values[1];
        var type = (PublicationType)inputWindow.Values[2];

        double price;
        try {
            if (string.IsNullOrWhiteSpace(pubIndex) || pubIndex.Length > 12 ||
                string.IsNullOrWhiteSpace(title) || title.Length > 100) {
                throw new InvalidDataException("Индекс - до 12 знаков, название - до 100 знаков, оба не пустые");
            } // if

            price = ParsePrice((string)inputWindow.Values[3]);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        var message = _catalog.AddPublication(pubIndex, title, type.Id, price);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        Publications = _catalog.Publications();
        TbcSelectedIndex = 0;
        Status = $"В каталог добавлено издание {pubIndex} «{title}», {price:n2} руб. в месяц";
    } // NewPublicationExec

    #endregion


    #region Изменить цену издания

    // выданные квитанции хранят цену на день оформления и не меняются
    private void ChangePriceExec(object obj) {
        Publications = _catalog.Publications();
        var inputWindow = new InputWindow("Новая цена издания",
            new InputField("Издание", Publications, "Title"),
            new InputField("Новая цена одного экземпляра за месяц, руб. (например, 120,50)")) {
            Owner = Application.Current.MainWindow
        };
        if (inputWindow.ShowDialog() != true) {
            return;
        } // if

        var publication = (Publication)inputWindow.Values[0];

        double price;
        try {
            price = ParsePrice((string)inputWindow.Values[1]);
        } catch (InvalidDataException ex) {
            Utils.Fail(ex.Message);
            return;
        } // try

        var message = _catalog.ChangePrice(publication.Id, price);
        if (message != null) {
            Utils.Fail(message);
            return;
        } // if

        Publications = _catalog.Publications();
        TbcSelectedIndex = 0;
        Status = $"Новая цена издания «{publication.Title}»: {price:n2} руб. в месяц; " +
                 "выданные квитанции сохраняют прежнюю цену";
    } // ChangePriceExec


    // цена из поля ввода - одно правило для нового издания и новой цены: число больше нуля
    // не больше чем с двумя знаками после запятой, иначе квитанция не сойдётся в копейках
    private static double ParsePrice(string text) {
        if (!double.TryParse(text.Replace(".", ","), out var price) || !double.IsFinite(price) ||
            price <= 0d || Math.Round(price, 2) != price) {
            throw new InvalidDataException("Цена - число больше нуля, не больше двух знаков после запятой, " +
                                           "например 120,50");
        } // if

        return price;
    } // ParsePrice

    #endregion

} // class MainWindowViewModel
