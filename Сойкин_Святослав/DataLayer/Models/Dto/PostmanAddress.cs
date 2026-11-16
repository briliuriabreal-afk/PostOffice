namespace DataLayer.Models.Dto;

// Запрос 2: почтальон, обслуживающий дом по адресу
public record PostmanAddress(
    string PostmanSurname,
    string PostmanShortName,
    int DistrictNumber,
    string Street,
    string Building
);
