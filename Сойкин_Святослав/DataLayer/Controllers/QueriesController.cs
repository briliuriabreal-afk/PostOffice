using System.Data;
using DataLayer.Models.Dto;
using Microsoft.Data.SqlClient;

namespace DataLayer.Controllers;

// Выборки через хранимые процедуры: запросы 1-6, справка, отчёт о доставке,
// кабинеты подписчика и почтальона, квитанция, списки для окон.
// Процедуры «на дату» получают дату от приложения - обычно сегодняшнюю.
// Столбцы читаются по номерам: процедуры перечисляют их явно, в этом порядке
public class QueriesController(string connectionString)
{

    #region Запросы задания

    // Запрос 1. Наименование и количество экземпляров всех изданий на дату
    public List<PublicationCopies> PublicationsCopies(DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("PublicationsCopies", connection);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        return BindToPublicationsCopies(reader);
    } // PublicationsCopies


    // Запрос 2. Почтальон, обслуживающий дом по адресу
    public List<PostmanAddress> PostmanByAddress(string street, string building) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("PostmanByAddress", connection);
        command.Parameters.AddWithValue("@street", street);
        command.Parameters.AddWithValue("@building", building);

        using var reader = command.ExecuteReader();
        return BindToPostmanAddresses(reader);
    } // PostmanByAddress


    // Запрос 3. Газеты, которые выписывает гражданин; отчество null - отчества нет
    public List<CitizenNewspaper> CitizenNewspapers(string surname, string name, string? patronymic,
        DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("CitizenNewspapers", connection);
        command.Parameters.AddWithValue("@surname", surname);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@patronymic", (object?)patronymic ?? DBNull.Value);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        return BindToCitizenNewspapers(reader);
    } // CitizenNewspapers


    // Запрос 4. Сколько почтальонов работает в отделении
    public int WorkingPostmenCount() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("WorkingPostmenCount", connection);
        return (int)command.ExecuteScalar();
    } // WorkingPostmenCount


    // Запрос 5. Участки с максимальным количеством экземпляров на дату
    public List<DistrictCopies> MaxCopiesDistricts(DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("MaxCopiesDistricts", connection);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        return BindToDistrictsCopies(reader);
    } // MaxCopiesDistricts


    // Запрос 6. Средний срок подписки по каждому изданию
    public List<AverageTerm> AverageTermByPublication() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("AverageTermByPublication", connection);

        using var reader = command.ExecuteReader();
        return BindToAverageTerms(reader);
    } // AverageTermByPublication


    // Справка: подписчики, газеты и журналы по подпискам, оформленным в текущем году
    public Summary SubscriptionsSummary(int year) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("SubscriptionsSummary", connection);
        command.Parameters.AddWithValue("@dateFrom", new DateTime(year, 1, 1));
        command.Parameters.AddWithValue("@dateTo", new DateTime(year, 12, 31));

        using var reader = command.ExecuteReader();
        reader.Read();
        return new Summary(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    } // SubscriptionsSummary


    // Отчёт о доставке: строки по участкам; итоги по изданию и участку считает приложение
    public List<DeliveryLine> DeliveryLines(DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("DeliveryLines", connection);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        return BindToDeliveryLines(reader);
    } // DeliveryLines


    // Отчёт о доставке: почтальоны, участки, различные издания
    public DeliveryTotals DeliveryTotals(DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("DeliveryTotals", connection);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        reader.Read();
        return new DeliveryTotals(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2));
    } // DeliveryTotals

    #endregion


    #region Кабинеты, квитанция, списки

    // Кабинет подписчика: его подписки и их состояние на дату
    public List<SubscriptionState> SubscriberHistory(int subscriberId, DateOnly onDate) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("SubscriberHistory", connection);
        command.Parameters.AddWithValue("@subscriberId", subscriberId);
        command.Parameters.AddWithValue("@onDate", onDate.ToDateTime(TimeOnly.MinValue));

        using var reader = command.ExecuteReader();
        return BindToSubscriptionStates(reader);
    } // SubscriberHistory


    // Кабинет почтальона: дома его участков
    public List<PostmanHouse> PostmanHouses(int postmanId) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("PostmanHouses", connection);
        command.Parameters.AddWithValue("@postmanId", postmanId);

        using var reader = command.ExecuteReader();
        return BindToPostmanHouses(reader);
    } // PostmanHouses


    // Данные квитанции по её номеру
    public Receipt Receipt(int subscriptionId) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("Receipt", connection);
        command.Parameters.AddWithValue("@subscriptionId", subscriptionId);

        using var reader = command.ExecuteReader();
        reader.Read();
        return new Receipt(
            reader.GetInt32(0),
            GetDate(reader, 1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            GetDate(reader, 10),
            GetDate(reader, 11),
            reader.GetInt32(12),
            reader.GetInt32(13),
            reader.GetDouble(14),
            reader.GetDouble(15)
        );
    } // Receipt


    // Участки и их почтальоны
    public List<District> Districts() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("DistrictsList", connection);

        using var reader = command.ExecuteReader();
        return BindToDistricts(reader);
    } // Districts


    // Работающие почтальоны и число их участков
    public List<PostmanLoad> WorkingPostmen() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("WorkingPostmenList", connection);

        using var reader = command.ExecuteReader();
        return BindToPostmenLoads(reader);
    } // WorkingPostmen


    // Подписчики с адресами
    public List<SubscriberAddress> Subscribers() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("SubscribersList", connection);

        using var reader = command.ExecuteReader();
        return BindToSubscriberAddresses(reader);
    } // Subscribers


    // Дома с адресами
    public List<HouseAddress> Houses() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("HousesList", connection);

        using var reader = command.ExecuteReader();
        return BindToHouseAddresses(reader);
    } // Houses


    // Граждане-подписчики по ФИО без повторов
    public List<Citizen> Citizens() {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("CitizensList", connection);

        using var reader = command.ExecuteReader();
        return BindToCitizens(reader);
    } // Citizens


    // Предварительная стоимость подписки по формуле базы и цене каталога
    public double OrderCost(int publicationId, int duration, int copies) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("OrderCost", connection);
        command.Parameters.AddWithValue("@publicationId", publicationId);
        command.Parameters.AddWithValue("@duration", duration);
        command.Parameters.AddWithValue("@copies", copies);

        return (double)command.ExecuteScalar();
    } // OrderCost


    // Человек по паспорту, null - человека с таким паспортом в базе нет
    public PersonInfo? PersonByPassport(string passport) {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = Procedure("PersonByPassport", connection);
        command.Parameters.AddWithValue("@passport", passport);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) {
            return null;
        } // if

        return new PersonInfo(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetInt32(4) > 0
        );
    } // PersonByPassport

    #endregion


    #region Привязка к записям

    private static List<PublicationCopies> BindToPublicationsCopies(SqlDataReader reader) {
        List<PublicationCopies> list = [];
        while (reader.Read()) {
            list.Add(new PublicationCopies(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4)
            ));
        } // while

        return list;
    } // BindToPublicationsCopies


    private static List<PostmanAddress> BindToPostmanAddresses(SqlDataReader reader) {
        List<PostmanAddress> list = [];
        while (reader.Read()) {
            list.Add(new PostmanAddress(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4)
            ));
        } // while

        return list;
    } // BindToPostmanAddresses


    private static List<CitizenNewspaper> BindToCitizenNewspapers(SqlDataReader reader) {
        List<CitizenNewspaper> list = [];
        while (reader.Read()) {
            list.Add(new CitizenNewspaper(
                reader.GetString(0),
                reader.GetString(1),
                GetDate(reader, 2),
                GetDate(reader, 3),
                reader.GetInt32(4),
                reader.GetString(5)
            ));
        } // while

        return list;
    } // BindToCitizenNewspapers


    private static List<DistrictCopies> BindToDistrictsCopies(SqlDataReader reader) {
        List<DistrictCopies> list = [];
        while (reader.Read()) {
            list.Add(new DistrictCopies(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2)
            ));
        } // while

        return list;
    } // BindToDistrictsCopies


    private static List<AverageTerm> BindToAverageTerms(SqlDataReader reader) {
        List<AverageTerm> list = [];
        while (reader.Read()) {
            list.Add(new AverageTerm(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3)
            ));
        } // while

        return list;
    } // BindToAverageTerms


    private static List<DeliveryLine> BindToDeliveryLines(SqlDataReader reader) {
        List<DeliveryLine> list = [];
        while (reader.Read()) {
            list.Add(new DeliveryLine(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                GetDate(reader, 5),
                GetDate(reader, 6),
                reader.GetInt32(7),
                reader.GetInt32(8)
            ));
        } // while

        return list;
    } // BindToDeliveryLines


    private static List<SubscriptionState> BindToSubscriptionStates(SqlDataReader reader) {
        List<SubscriptionState> list = [];
        while (reader.Read()) {
            list.Add(new SubscriptionState(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                GetDate(reader, 4),
                GetDate(reader, 5),
                GetDate(reader, 6),
                reader.GetInt32(7),
                reader.GetInt32(8),
                reader.GetDouble(9),
                reader.GetDouble(10),
                reader.GetString(11)
            ));
        } // while

        return list;
    } // BindToSubscriptionStates


    private static List<PostmanHouse> BindToPostmanHouses(SqlDataReader reader) {
        List<PostmanHouse> list = [];
        while (reader.Read()) {
            list.Add(new PostmanHouse(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        } // while

        return list;
    } // BindToPostmanHouses


    private static List<District> BindToDistricts(SqlDataReader reader) {
        List<District> list = [];
        while (reader.Read()) {
            list.Add(new District(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                reader.GetString(3)
            ));
        } // while

        return list;
    } // BindToDistricts


    private static List<PostmanLoad> BindToPostmenLoads(SqlDataReader reader) {
        List<PostmanLoad> list = [];
        while (reader.Read()) {
            list.Add(new PostmanLoad(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2)
            ));
        } // while

        return list;
    } // BindToPostmenLoads


    private static List<SubscriberAddress> BindToSubscriberAddresses(SqlDataReader reader) {
        List<SubscriberAddress> list = [];
        while (reader.Read()) {
            list.Add(new SubscriberAddress(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5)
            ));
        } // while

        return list;
    } // BindToSubscriberAddresses


    private static List<HouseAddress> BindToHouseAddresses(SqlDataReader reader) {
        List<HouseAddress> list = [];
        while (reader.Read()) {
            list.Add(new HouseAddress(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4)
            ));
        } // while

        return list;
    } // BindToHouseAddresses


    private static List<Citizen> BindToCitizens(SqlDataReader reader) {
        List<Citizen> list = [];
        while (reader.Read()) {
            list.Add(new Citizen(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetString(3)
            ));
        } // while

        return list;
    } // BindToCitizens

    #endregion


    // команда вызова хранимой процедуры
    private static SqlCommand Procedure(string name, SqlConnection connection) =>
        new(name, connection) { CommandType = CommandType.StoredProcedure };


    // дата из столбца типа date
    private static DateOnly GetDate(SqlDataReader reader, int column) =>
        DateOnly.FromDateTime(reader.GetDateTime(column));

} // class QueriesController
