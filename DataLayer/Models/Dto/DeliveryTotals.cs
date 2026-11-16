namespace DataLayer.Models.Dto;

// Отчёт о доставке: итоги по отделению
public record DeliveryTotals(
    int Postmen,
    int Districts,
    int Publications
);
