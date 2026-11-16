using System.IO;

namespace PostOffice.Models;

// Анкета нового клиента: человек, домашний адрес и обязательные логин и пароль
public class ClientCard : PersonCard
{
    // дом из справочника отделения, null - дом не выбран
    public int? HouseId { get; set; }

    // квартира, null - частный дом
    public int? Flat { get; private set; }

    // квартира так, как она введена в поле
    public string FlatText {
        get => Flat == null ? "" : $"{Flat}";
        set => Flat = ParseFlat(value);
    } // FlatText


    // номер квартиры из поля ввода: пустое поле - частный дом
    public static int? ParseFlat(string text) {
        if (text.Length == 0) {
            return null;
        } // if

        if (!int.TryParse(text, out var flat) || flat <= 0) {
            throw new InvalidDataException(
                "Номер квартиры - целое число больше нуля; для частного дома оставьте поле пустым");
        } // if

        return flat;
    } // ParseFlat


    // анкета заполнена: дом выбран, учётная запись задана по правилам
    public void Check() {
        if (HouseId == null) {
            throw new InvalidDataException("Выберите улицу и дом");
        } // if

        CheckAccount(true);
    } // Check

} // class ClientCard
