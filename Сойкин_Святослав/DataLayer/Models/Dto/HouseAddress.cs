namespace DataLayer.Models.Dto;

// Дом с адресом - выбор адреса в запросе 2
public record HouseAddress(
    int Id,
    string Street,
    string Building,
    string Address,
    int DistrictNumber
);
