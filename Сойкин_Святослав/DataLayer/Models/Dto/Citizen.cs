namespace DataLayer.Models.Dto;

// Гражданин-подписчик по ФИО - выбор гражданина в запросе 3
public record Citizen(
    string Surname,
    string Name,
    string? Patronymic,
    string FullName
);
