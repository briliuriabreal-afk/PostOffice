using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Controllers;

// Каталог изданий, справочник улиц с домами, адрес подписчика - через EF Core.
// Изменения возвращают фразу отказа или null, если изменение выполнено;
// формат полей (не пусто, цена больше нуля) проверяет форма ввода
public class CatalogController
{

    // каталог изданий с видами, по названию
    public List<Publication> Publications() {
        using var db = new PostOfficeContext();
        return [.. db.Publications
            .Include(publication => publication.PublicationType)
            .OrderBy(publication => publication.Title)
        ];
    } // Publications


    // виды изданий
    public List<PublicationType> PublicationTypes() {
        using var db = new PostOfficeContext();
        return [.. db.PublicationTypes.OrderBy(type => type.Name)];
    } // PublicationTypes


    // новое подписное издание; индекс издания не повторяется
    public string? AddPublication(string pubIndex, string title, int publicationTypeId, double price) {
        using var db = new PostOfficeContext();

        if (db.Publications.Any(publication => publication.PubIndex == pubIndex)) {
            return $"Издание с индексом {pubIndex} уже есть в каталоге";
        } // if

        db.Publications.Add(new Publication {
            PubIndex = pubIndex,
            Title = title,
            PublicationTypeId = publicationTypeId,
            Price = price
        });
        db.SaveChanges();
        return null;
    } // AddPublication


    // новая цена издания; выданные квитанции хранят свою цену и не меняются
    public string? ChangePrice(int publicationId, double price) {
        using var db = new PostOfficeContext();

        var publication = db.Publications.Find(publicationId);
        if (publication == null) {
            return "Издания нет в каталоге";
        } // if

        publication.Price = price;
        db.SaveChanges();
        return null;
    } // ChangePrice


    // улицы отделения с их домами - выбор адреса: сначала улица, затем дом на ней
    public List<Street> Streets() {
        using var db = new PostOfficeContext();
        return [.. db.Streets
            .Include(street => street.Houses.OrderBy(house => house.Building))
            .OrderBy(street => street.Name)
        ];
    } // Streets


    // новый адрес подписчика: подписки идут за ним, участок меняется вместе с домом
    public string? ChangeAddress(int subscriberId, int houseId, int? flat) {
        using var db = new PostOfficeContext();

        var subscriber = db.Subscribers.Find(subscriberId);
        if (subscriber == null) {
            return "Подписчик не найден";
        } // if

        subscriber.HouseId = houseId;
        subscriber.Flat = flat;
        db.SaveChanges();
        return null;
    } // ChangeAddress

} // class CatalogController
