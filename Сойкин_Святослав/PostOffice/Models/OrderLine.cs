using DataLayer.Models;

namespace PostOffice.Models;

// Строка заказа подписки: издание, начало, срок, экземпляры и предварительная стоимость
public record OrderLine(
    Publication Publication,
    DateOnly DateStart,
    int Duration,
    int Copies,
    double Cost
);
