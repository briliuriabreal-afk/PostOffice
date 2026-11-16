namespace DataLayer.Models.Dto;

// Запрос 5: участок и сумма экземпляров действующих подписок
public record DistrictCopies(
    int DistrictNumber,
    string PostmanShortName,
    int Copies
);
