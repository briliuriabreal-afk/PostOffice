using System.IO;

namespace PostOffice.Models;

// Анкета принимаемого почтальона: человек, учётная запись и выделяемый участок
public class PostmanCard : PersonCard
{
    // участок, который переходит к новому почтальону, null - участок не выбран
    public int? DistrictId { get; set; }


    // анкета заполнена: участок выбран; логин нужен, если учётной записи ещё нет
    public void Check(bool loginRequired) {
        if (DistrictId == null) {
            throw new InvalidDataException("Выберите участок");
        } // if

        CheckAccount(loginRequired);
    } // Check

} // class PostmanCard
