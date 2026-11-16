namespace DataLayer.Models.Dto;

// Человек по паспорту при приёме на работу: ФИО, работает ли он сейчас
// в отделении; логин и хеш есть у каждого человека
public record PersonInfo(
    int Id,
    string Surname,
    string Name,
    string? Patronymic,
    bool IsWorking
);
