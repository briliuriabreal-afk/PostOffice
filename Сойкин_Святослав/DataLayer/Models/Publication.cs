namespace DataLayer.Models;

// Подписное издание каталога
public class Publication
{
    public int Id { get; set; }

    // подписной индекс
    public string PubIndex { get; set; } = "";

    // название
    public string Title { get; set; } = "";

    public int PublicationTypeId { get; set; }
    public PublicationType PublicationType { get; set; } = null!;

    // цена одного экземпляра за месяц
    public double Price { get; set; }

} // class Publication
