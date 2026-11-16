namespace DataLayer.Models.Dto;

// Запрос 3: газета, которую выписывает гражданин
public record CitizenNewspaper(
    string PubIndex,
    string Title,
    DateOnly DateStart,
    DateOnly DateEnd,
    int Copies,
    string Address
);
