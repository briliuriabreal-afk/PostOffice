namespace DataLayer.Models;

// Вид издания: газета, журнал
public class PublicationType
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    // издания этого вида
    public List<Publication> Publications { get; set; } = [];

} // class PublicationType
