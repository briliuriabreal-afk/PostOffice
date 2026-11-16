namespace DataLayer.Models;

// Человек: общие персональные данные работников и подписчиков
public class Person
{
    public int Id { get; set; }

    public string Surname { get; set; } = "";
    public string Name { get; set; } = "";

    // null - отчества нет
    public string? Patronymic { get; set; }

    // серия и номер паспорта
    public string Passport { get; set; } = "";

} // class Person
