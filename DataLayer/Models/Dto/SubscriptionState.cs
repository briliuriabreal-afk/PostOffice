namespace DataLayer.Models.Dto;

// Кабинет подписчика: подписка и её состояние на дату
public record SubscriptionState(
    int Id,
    string PubIndex,
    string Title,
    string PublicationType,
    DateOnly IssueDate,
    DateOnly DateStart,
    DateOnly DateEnd,
    int Duration,
    int Copies,
    double Price,
    double Cost,
    string Status
);
