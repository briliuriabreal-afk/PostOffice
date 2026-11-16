namespace DataLayer.Models.Dto;

// Справка: подписчики, газеты и журналы по подпискам, оформленным в периоде
public record Summary(
    int Subscribers,
    int Newspapers,
    int Magazines
);
