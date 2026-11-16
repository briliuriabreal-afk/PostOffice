namespace DataLayer.Models.Dto;

// Запрос 1: экземпляры издания, получаемые отделением на дату
public record PublicationCopies(
    int Id,
    string PubIndex,
    string Title,
    string PublicationType,
    int Copies
);
