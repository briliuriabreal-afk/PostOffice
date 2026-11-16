namespace DataLayer.Models.Dto;

// Квитанция: подписка на одно издание, номер квитанции - Id
public record Receipt(
    int Id,
    DateOnly IssueDate,
    string Surname,
    string Name,
    string? Patronymic,
    string Address,
    string OperatorShortName,
    string PubIndex,
    string Title,
    string PublicationType,
    DateOnly DateStart,
    DateOnly DateEnd,
    int Duration,
    int Copies,
    double Price,
    double Cost
);
