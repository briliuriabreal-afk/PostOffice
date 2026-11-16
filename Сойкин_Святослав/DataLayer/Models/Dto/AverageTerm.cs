namespace DataLayer.Models.Dto;

// Запрос 6: средний срок подписки на издание, месяцев
public record AverageTerm(
    int PublicationId,
    string PubIndex,
    string Title,
    double AverageDuration
);
