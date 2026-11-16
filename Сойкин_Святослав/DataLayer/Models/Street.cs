namespace DataLayer.Models;

// Улица, на которой стоят дома отделения; тип улицы входит в название
public class Street
{
    public int Id { get; set; }

    // «ул. Ленина», «пр. Мира»
    public string Name { get; set; } = "";

    // дома улицы
    public List<House> Houses { get; set; } = [];

} // class Street
