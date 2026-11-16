using DataLayer.Models.Dto;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PostOffice.Documents;

// Квитанция о подписке в PDF: номер и дата оформления, подписчик и адрес доставки,
// что выписано и на какой срок, общая стоимость подписки. Все значения берутся
// из базы: условия подписки сохранены, ФИО и адрес доставки - текущие
public class ReceiptDocument(Receipt receipt) : IDocument
{
    // фамилия, имя и отчество подписчика полностью
    private string FullName => receipt.Patronymic == null
        ? $"{receipt.Surname} {receipt.Name}"
        : $"{receipt.Surname} {receipt.Name} {receipt.Patronymic}";


    public void Compose(IDocumentContainer container) {
        container.Page(page => {
            page.Size(PageSizes.A5.Landscape());
            page.Margin(1.2f, Unit.Centimetre);
            // шрифт - встроенный в библиотеку Lato: документ выглядит одинаково на любом компьютере
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    } // Compose


    // номер квитанции и дата оформления
    private void ComposeHeader(IContainer container) {
        container.Column(column => {
            column.Item().Text("Почтовое отделение").FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text($"Квитанция № {receipt.Id} о подписке на периодическое издание").FontSize(14).Bold();
            column.Item().Text($"Дата оформления: {receipt.IssueDate:dd.MM.yyyy}");
        });
    } // ComposeHeader


    // подписчик, что выписано, на какой срок и общая стоимость
    private void ComposeContent(IContainer container) {
        container.PaddingVertical(10).Column(column => {
            column.Spacing(4);

            column.Item().Text(text => {
                text.Span("Подписчик: ").Bold();
                text.Span(FullName);
            });
            column.Item().Text(text => {
                text.Span("Адрес доставки: ").Bold();
                text.Span(receipt.Address);
            });

            column.Item().PaddingTop(8).Table(table => {
                table.ColumnsDefinition(columns => {
                    columns.ConstantColumn(45);     // индекс
                    columns.RelativeColumn(2.6f);   // издание
                    columns.RelativeColumn(1.1f);   // вид
                    columns.RelativeColumn(3.6f);   // срок подписки - одной строкой
                    columns.ConstantColumn(32);     // месяцев
                    columns.ConstantColumn(32);     // экземпляров
                    columns.RelativeColumn(1.4f);   // цена за месяц
                    columns.RelativeColumn(1.5f);   // стоимость
                });

                table.Header(header => {
                    header.Cell().Element(HeaderCell).Text("Индекс");
                    header.Cell().Element(HeaderCell).Text("Издание");
                    header.Cell().Element(HeaderCell).Text("Вид");
                    header.Cell().Element(HeaderCell).Text("Срок подписки");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Мес.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Экз.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Цена за месяц, руб.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Стоимость, руб.");
                });

                table.Cell().Element(Cell).Text(receipt.PubIndex);
                table.Cell().Element(Cell).Text(receipt.Title);
                table.Cell().Element(Cell).Text(receipt.PublicationType);
                table.Cell().Element(Cell).Text($"с {receipt.DateStart:dd.MM.yyyy} по {receipt.DateEnd:dd.MM.yyyy}");
                table.Cell().Element(Cell).AlignRight().Text($"{receipt.Duration}");
                table.Cell().Element(Cell).AlignRight().Text($"{receipt.Copies}");
                table.Cell().Element(Cell).AlignRight().Text($"{receipt.Price:n2}");
                table.Cell().Element(Cell).AlignRight().Text($"{receipt.Cost:n2}");
            });

            column.Item().PaddingTop(8).AlignRight()
                .Text($"Итого к оплате: {receipt.Cost:n2} руб.").FontSize(13).Bold();
        });
    } // ComposeContent


    // кто оформил подписку, место для подписей
    private void ComposeFooter(IContainer container) {
        container.Row(row => {
            row.RelativeItem().Text($"Оформил: оператор {receipt.OperatorShortName}  ____________");
            row.RelativeItem().AlignRight().Text("Подпись подписчика  ____________");
        });
    } // ComposeFooter


    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(3).DefaultTextStyle(text => text.Bold());


    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);

} // class ReceiptDocument
