using System.IO;

namespace PostOffice.Models;

// Анкета человека в окне ввода: паспорт, ФИО и учётная запись.
// Свойство само отклоняет неверное значение исключением: окно показывает
// причину рамкой поля и сообщением, неверное значение в анкету не попадает
public class PersonCard
{
    // наименьшая длина логина и пароля
    public const int MinLoginLength = 3;
    public const int MinPasswordLength = 6;


    // серия и номер паспорта
    private string _passport = "";
    public string Passport {
        get => _passport;
        set => _passport = string.IsNullOrWhiteSpace(value) || value.Length > 15
            ? throw new InvalidDataException("Введите серию и номер паспорта, не длиннее 15 знаков")
            : value;
    } // Passport

    private string _surname = "";
    public string Surname {
        get => _surname;
        set => _surname = string.IsNullOrWhiteSpace(value) || value.Length > 50
            ? throw new InvalidDataException("Введите фамилию, не длиннее 50 знаков")
            : value;
    } // Surname

    private string _name = "";
    public string Name {
        get => _name;
        set => _name = string.IsNullOrWhiteSpace(value) || value.Length > 40
            ? throw new InvalidDataException("Введите имя, не длиннее 40 знаков")
            : value;
    } // Name

    // отчество, null - отчества нет; пустое поле означает, что отчества нет
    private string? _patronymic;
    public string? Patronymic {
        get => _patronymic;
        set {
            if (value != null && (value.Contains(' ') || value.Length > 50)) {
                throw new InvalidDataException("Отчество - одно слово без пробелов, не длиннее 50 знаков");
            } // if

            _patronymic = string.IsNullOrEmpty(value) ? null : value;
        } // set
    } // Patronymic

    // логин, null - ещё не введён
    private string? _login;
    public string? Login {
        get => _login;
        set {
            if (!string.IsNullOrEmpty(value)) {
                CheckLogin(value);
            } // if

            _login = string.IsNullOrEmpty(value) ? null : value;
        } // set
    } // Login

    // пароль, null - не задан
    private string? _password;
    public string? Password {
        get => _password;
        set {
            if (!string.IsNullOrEmpty(value)) {
                CheckPassword(value);
            } // if

            _password = string.IsNullOrEmpty(value) ? null : value;
        } // set
    } // Password


    // требования к логину и паролю - одни для анкет и смены пароля
    public static void CheckLogin(string login) {
        if (login.Contains(' ') || login.Length < MinLoginLength || login.Length > 30) {
            throw new InvalidDataException($"Логин - от {MinLoginLength} до 30 знаков без пробелов");
        } // if
    } // CheckLogin

    public static void CheckPassword(string password) {
        if (password.Length < MinPasswordLength || password.Length > 50) {
            throw new InvalidDataException($"Пароль - от {MinPasswordLength} до 50 знаков");
        } // if
    } // CheckPassword


    // новому человеку нужны логин и пароль; при приёме существующего человека
    // сохраняется его прежний вход, поэтому новые данные формы не требуются
    protected void CheckAccount(bool loginRequired) {
        if (loginRequired && (Login == null || Password == null)) {
            throw new InvalidDataException("Введите логин и пароль: они обязательны для нового человека");
        } // if

        if (Login == null && Password != null) {
            throw new InvalidDataException("Пароль задаётся вместе с логином: введите логин или очистите поле пароля");
        } // if
    } // CheckAccount

} // class PersonCard
