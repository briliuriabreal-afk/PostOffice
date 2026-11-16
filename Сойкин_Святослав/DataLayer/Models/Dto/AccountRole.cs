namespace DataLayer.Models.Dto;

// Роль учётной записи; работник-подписчик получает две роли
public record AccountRole(
    int PersonId,
    int? EmployeeId,
    int? SubscriberId,
    string Login,
    string ShortName,
    string Role
);
