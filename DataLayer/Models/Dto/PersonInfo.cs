namespace DataLayer.Models.Dto;

// Человек по паспорту при приёме на работу: ФИО, работает ли он сейчас
// в отделении и есть ли у него учётная запись
public record PersonInfo(
    int Id,
    string Surname,
    string Name,
    string? Patronymic,
    bool IsWorking,
    bool HasAccount
);
