using System.Data;
using DataLayer.Infrastructure;
using DataLayer.Models.Dto;
using Microsoft.Data.SqlClient;

namespace DataLayer.Controllers;

// Операции через хранимые процедуры: вход, регистрация, учётные записи, оформление
// подписки, приём, передача участка, увольнение. Ошибку пользователя процедура
// возвращает фразой в выходном параметре @message и ничего не записывает:
// Message == null - операция выполнена
public class OperationsController(string connectionString)
{

    // Вход: роли учётной записи; работник-подписчик получает две роли
    public (List<AccountRole> Roles, string? Message) CheckLogin(string login, string password) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("CheckLogin", connection);
        command.Parameters.AddWithValue("@login", login);
        AddHash(command, "@passwordHash", SavedLogin(login, connection), password);
        var message = AddMessage(command);

        List<AccountRole> roles = [];
        using (var reader = command.ExecuteReader()) {
            while (reader.Read()) {
                roles.Add(new AccountRole(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5)
                ));
            } // while
        } // using

        // выходной параметр доступен после закрытия выборки
        return (roles, message.Value as string);
    } // CheckLogin


    // Регистрация подписчика; каждый человек получает логин и хеш пароля
    public (int? SubscriberId, string? Message) RegisterSubscriber(string passport, string surname,
        string name, string? patronymic, int houseId, int? flat, string? login, string? password) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("RegisterSubscriber", connection);
        command.Parameters.AddWithValue("@passport", passport);
        command.Parameters.AddWithValue("@surname", surname);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@patronymic", (object?)patronymic ?? DBNull.Value);
        command.Parameters.AddWithValue("@houseId", houseId);
        command.Parameters.AddWithValue("@flat", (object?)flat ?? DBNull.Value);
        command.Parameters.AddWithValue("@login", (object?)login ?? DBNull.Value);
        AddHash(command, "@passwordHash", SavedLogin(login, connection), password);
        var subscriberId = AddId(command, "@subscriberId");
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return (subscriberId.Value as int?, message.Value as string);
    } // RegisterSubscriber


    // Смена пароля пользователем
    public string? ChangePassword(string login, string oldPassword, string newPassword) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("ChangePassword", connection);
        command.Parameters.AddWithValue("@login", login);
        var savedLogin = SavedLogin(login, connection);
        AddHash(command, "@oldPasswordHash", savedLogin, oldPassword);
        AddHash(command, "@newPasswordHash", savedLogin, newPassword);
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return message.Value as string;
    } // ChangePassword


    // Оформление подписки; номер квитанции - номер подписки
    public (int? SubscriptionId, string? Message) CreateSubscription(int subscriberId, int operatorId,
        int publicationId, DateOnly dateStart, int duration, int copies) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("CreateSubscription", connection);
        command.Parameters.AddWithValue("@subscriberId", subscriberId);
        command.Parameters.AddWithValue("@operatorId", operatorId);
        command.Parameters.AddWithValue("@publicationId", publicationId);
        command.Parameters.AddWithValue("@dateStart", dateStart.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@duration", duration);
        command.Parameters.AddWithValue("@copies", copies);
        var subscriptionId = AddId(command, "@subscriptionId");
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return (subscriptionId.Value as int?, message.Value as string);
    } // CreateSubscription


    // Приём почтальона с выделением участка; логин null - у человека уже есть учётная запись
    public (int? EmployeeId, string? Message) HirePostman(string passport, string surname, string name,
        string? patronymic, string? login, string? password, int districtId) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("HirePostman", connection);
        command.Parameters.AddWithValue("@passport", passport);
        command.Parameters.AddWithValue("@surname", surname);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@patronymic", (object?)patronymic ?? DBNull.Value);
        command.Parameters.AddWithValue("@login", (object?)login ?? DBNull.Value);
        AddHash(command, "@passwordHash", SavedLogin(login, connection), password);
        command.Parameters.AddWithValue("@districtId", districtId);
        var employeeId = AddId(command, "@employeeId");
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return (employeeId.Value as int?, message.Value as string);
    } // HirePostman


    // Передача участка работающему почтальону
    public string? ReassignDistrict(int districtId, int postmanId) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("ReassignDistrict", connection);
        command.Parameters.AddWithValue("@districtId", districtId);
        command.Parameters.AddWithValue("@postmanId", postmanId);
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return message.Value as string;
    } // ReassignDistrict


    // Увольнение почтальона с передачей его участков
    public string? FirePostman(int postmanId) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("FirePostman", connection);
        command.Parameters.AddWithValue("@postmanId", postmanId);
        var message = AddMessage(command);

        command.ExecuteNonQuery();
        return message.Value as string;
    } // FirePostman


    // SQL сравнивает логины без учёта регистра; хеш использует сохранённое написание
    private static string? SavedLogin(string? login, SqlConnection connection) {
        using var command = new SqlCommand("select [Login] from People where [Login] = @login", connection);
        command.Parameters.AddWithValue("@login", (object?)login ?? DBNull.Value);
        return command.ExecuteScalar() as string ?? login;
    } // SavedLogin


    // В SQL передаётся только готовый хеш, открытый пароль остаётся в приложении
    private static void AddHash(SqlCommand command, string name, string? login, string? password) {
        var hash = PasswordHasher.Compute(login, password);
        command.Parameters.Add(name, SqlDbType.VarBinary, 32).Value = (object?)hash ?? DBNull.Value;
    } // AddHash


    // команда вызова хранимой процедуры
    private static SqlCommand Procedure(string name, SqlConnection connection) =>
        new(name, connection) { CommandType = CommandType.StoredProcedure };


    // выходной параметр с причиной отказа
    private static SqlParameter AddMessage(SqlCommand command) {
        var message = command.Parameters.Add("@message", SqlDbType.NVarChar, 200);
        message.Direction = ParameterDirection.Output;
        return message;
    } // AddMessage


    // выходной параметр с номером созданной записи
    private static SqlParameter AddId(SqlCommand command, string name) {
        var id = command.Parameters.Add(name, SqlDbType.Int);
        id.Direction = ParameterDirection.Output;
        return id;
    } // AddId

} // class OperationsController
