namespace DataLayer.Models.Dto;

// Работающий почтальон и число его участков
public record PostmanLoad(
    int Id,
    string ShortName,
    int DistrictCount
);
