using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using DataLayer.Controllers;
using DataLayer.Infrastructure;
using DataLayer.Models;
using DataLayer.Models.Dto;
using PostOffice.Infrastructure;
using PostOffice.Models;

namespace PostOffice.ViewModels;

// Модель представления окна оформления подписки: клиент и заказ из нескольких
// изданий, у каждого своё начало, срок и экземпляры. Стоимость считает база
// по формуле SubscriptionCost и цене каталога. Каждая строка заказа оформляется
// отдельной подпиской со своей квитанцией - после закрытия окна
public class SubscriptionWindowViewModel : INotifyPropertyChanged
{
    private readonly QueriesController _queries = new(Connection.ConnectionString);


    #region Списки окна

    public List<SubscriberAddress> Subscribers { get; }
    public List<Publication> Publications { get; }

    // начало подписки - 1-е число одного из 12 следующих месяцев
    public List<DateOnly> Months { get; }

    // допустимые сроки в месяцах и количества экземпляров
    public List<int> Durations { get; } = [.. Enumerable.Range(1, 12)];
    public List<int> CopiesValues { get; } = [.. Enumerable.Range(1, 10)];

    #endregion


    #region Клиент и издание, которое добавляется в заказ

    private SubscriberAddress? _subscriber;
    public SubscriberAddress? Subscriber {
        get => _subscriber;
        set => SetField(ref _subscriber, value);
    } // Subscriber

    private Publication? _publication;
    public Publication? Publication {
        get => _publication;
        set {
            SetField(ref _publication, value);
            UpdateCost();
        } // set
    } // Publication

    private DateOnly _dateStart;
    public DateOnly DateStart {
        get => _dateStart;
        set => SetField(ref _dateStart, value);
    } // DateStart

    private int _duration = 1;
    public int Duration {
        get => _duration;
        set {
            SetField(ref _duration, value);
            UpdateCost();
        } // set
    } // Duration

    private int _copies = 1;
    public int Copies {
        get => _copies;
        set {
            SetField(ref _copies, value);
            UpdateCost();
        } // set
    } // Copies

    // стоимость выбранного издания на выбранный срок
    private double _cost;
    public double Cost {
        get => _cost;
        private set => SetField(ref _cost, value);
    } // Cost

    #endregion


    #region Заказ

    private List<OrderLine> _lines = [];
    public List<OrderLine> Lines {
        get => _lines;
        private set {
            SetField(ref _lines, value);
            OnPropertyChanged(nameof(Total));
        } // set
    } // Lines

    // общая стоимость заказа
    public double Total => Lines.Sum(line => line.Cost);

    // добавить выбранное издание в заказ
    public RelayCommand AddLineCommand { get; }

    // убрать из заказа строку, выбранную в таблице
    public RelayCommand RemoveLineCommand { get; }

    #endregion


    // месяцы начала отсчитываются от сегодняшней даты: подписка начинается
    // не раньше следующего месяца и не позже чем через 12 месяцев
    public SubscriptionWindowViewModel(DateOnly today, List<SubscriberAddress> subscribers,
        List<Publication> publications) {
        Subscribers = subscribers;
        Publications = publications;

        var firstDay = new DateOnly(today.Year, today.Month, 1);
        Months = [.. Enumerable.Range(1, 12).Select(month => firstDay.AddMonths(month))];
        _dateStart = Months[0];

        AddLineCommand = new RelayCommand(execute: obj => AddLine(), canExecute: obj => Publication != null);
        RemoveLineCommand = new RelayCommand(execute: obj => RemoveLine((OrderLine)obj),
            canExecute: obj => obj is OrderLine);
    } // SubscriptionWindowViewModel


    // заказ готов к оформлению. Заказ из одного издания оформляется сразу,
    // без кнопки «Добавить в заказ»; выбранное издание, которого в заказе нет,
    // не отбрасывается молча
    public void Complete() {
        if (Subscriber == null) {
            throw new InvalidDataException("Выберите клиента");
        } // if

        if (Lines.Count == 0) {
            AddLine();
            return;
        } // if

        if (Publication != null && Lines.All(line => line.Publication != Publication)) {
            throw new InvalidDataException($"Издание «{Publication.Title}» выбрано, но не добавлено в заказ: " +
                                           "добавьте его кнопкой «Добавить в заказ» или выберите в списке " +
                                           "издание, которое уже есть в заказе");
        } // if
    } // Complete


    private void AddLine() {
        if (Publication == null) {
            throw new InvalidDataException("Выберите издание");
        } // if

        Lines = [.. Lines, new OrderLine(Publication, DateStart, Duration, Copies, Cost)];
    } // AddLine


    // одинаковые строки убираются по одной
    private void RemoveLine(OrderLine line) {
        List<OrderLine> lines = [.. Lines];
        lines.Remove(line);
        Lines = lines;
    } // RemoveLine


    private void UpdateCost() =>
        Cost = Publication == null ? 0d : _queries.OrderCost(Publication.Id, Duration, Copies);


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

} // class SubscriptionWindowViewModel
