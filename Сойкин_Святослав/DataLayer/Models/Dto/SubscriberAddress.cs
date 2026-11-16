namespace DataLayer.Models.Dto;

// Подписчик с домашним адресом и участком
public record SubscriberAddress(
    int Id,
    int PersonId,
    string ShortName,
    string Passport,
    string Address,
    int DistrictNumber
);
