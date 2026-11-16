namespace DataLayer.Models;

// Дом, который обслуживает отделение; дом входит в участок доставки
public class House
{
    public int Id { get; set; }

    public int StreetId { get; set; }
    public Street Street { get; set; } = null!;

    // номер дома: «12», «14а», «22к1»
    public string Building { get; set; } = "";

    // участок, в который входит дом
    public int DistrictId { get; set; }

} // class House
