namespace DataLayer.Models.Dto;

// Участок и его почтальон
public record District(
    int Id,
    int PostmanId,
    int Number,
    string PostmanShortName
);
