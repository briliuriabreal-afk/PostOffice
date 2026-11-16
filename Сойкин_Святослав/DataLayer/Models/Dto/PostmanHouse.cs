namespace DataLayer.Models.Dto;

// Кабинет почтальона: дом его участка
public record PostmanHouse(
    int DistrictNumber,
    string Street,
    string Building
);
