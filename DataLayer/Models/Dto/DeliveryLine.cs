namespace DataLayer.Models.Dto;

// Отчёт о доставке: строка - издание по адресу доставки на участке
public record DeliveryLine(
    int DistrictNumber,
    string PostmanShortName,
    string PubIndex,
    string Title,
    string Address,
    DateOnly DateStart,
    DateOnly DateEnd,
    int Duration,
    int Copies
);
