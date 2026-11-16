namespace DataLayer.Models;

// Подписчик - надстройка над человеком: домашний адрес
public class Subscriber
{
    public int Id { get; set; }

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int HouseId { get; set; }
    public House House { get; set; } = null!;

    // квартира, null - частный дом
    public int? Flat { get; set; }

} // class Subscriber
