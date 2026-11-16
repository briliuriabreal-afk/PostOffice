using DataLayer.Models.Dto;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PostOffice.Documents;

// Отчёт о доставке газет и журналов в PDF, упорядоченный по участкам.
// Для участка - почтальон и перечень доставляемых изданий с адресами доставки;
// под каждым изданием итог - средний срок подписки и суммарное количество экземпляров;
// по участку - число различных изданий; в конце - итоги по отделению.
// Строки приходят из базы упорядоченными по участку, изданию и адресу,
// группирует их сам отчёт
public class DeliveryReportDocument(DateOnly onDate, List<DeliveryLine> lines, DeliveryTotals totals)
    : IDocument
{

    public void Compose(IDocumentContainer container) {
        container.Page(page => {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            // шрифт - встроенный в библиотеку Lato: документ выглядит одинаково на любом компьютере
            page.DefaultTextStyle(text => text.FontSize(9));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().AlignCenter().Text(text => {
                text.Span("Страница ");
                text.CurrentPageNumber();
                text.Span(" из ");
                text.TotalPages();
            });
        });
    } // Compose


    private void ComposeHeader(IContainer container) {
        container.PaddingBottom(8).Column(column => {
            column.Item().Text("Почтовое отделение").FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text($"Отчёт о доставке газет и журналов на {onDate:dd.MM.yyyy}").FontSize(14).Bold();
        });
    } // ComposeHeader


    // участки по порядку номеров, затем итоги по отделению
    private void ComposeContent(IContainer container) {
        container.Column(column => {
            column.Spacing(14);

            // участок начинается с новой страницы, если на текущей не осталось места
            // хотя бы на заголовок и несколько строк
            foreach (var district in lines.GroupBy(line => line.DistrictNumber)) {
                column.Item().EnsureSpace(120).Element(item => ComposeDistrict(item, district));
            } // foreach district

            // итоги по отделению не разрываются между страницами
            column.Item().ShowEntire().Element(ComposeTotals);
        });
    } // ComposeContent


    // участок: почтальон, издания с адресами и итогами, число различных изданий.
    // Номер участка и почтальон - в шапке таблицы: она повторяется на каждой странице,
    // по которой проходит участок
    private static void ComposeDistrict(IContainer container, IGrouping<int, DeliveryLine> district) {
        var publications = district.GroupBy(line => line.PubIndex).ToList();

        container.Column(column => {
            column.Item().Table(table => {
                table.ColumnsDefinition(columns => {
                    columns.ConstantColumn(45);     // индекс
                    columns.RelativeColumn(2.4f);   // издание
                    columns.RelativeColumn(3);      // адрес доставки
                    columns.RelativeColumn(2.2f);   // срок подписки
                    columns.ConstantColumn(40);     // месяцев
                    columns.ConstantColumn(40);     // экземпляров
                });

                table.Header(header => {
                    header.Cell().ColumnSpan(6).PaddingBottom(4)
                        .Text($"Участок № {district.Key}, почтальон {district.First().PostmanShortName}")
                        .FontSize(11).Bold();

                    header.Cell().Element(HeaderCell).Text("Индекс");
                    header.Cell().Element(HeaderCell).Text("Издание");
                    header.Cell().Element(HeaderCell).Text("Адрес доставки");
                    header.Cell().Element(HeaderCell).Text("Срок подписки");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Мес.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Экз.");
                });

                foreach (var publication in publications) {
                    // индекс и название издания - только в первой его строке
                    var isFirst = true;
                    foreach (var line in publication) {
                        table.Cell().Element(Cell).Text(isFirst ? line.PubIndex : "");
                        table.Cell().Element(Cell).Text(isFirst ? line.Title : "");
                        table.Cell().Element(Cell).Text(line.Address);
                        table.Cell().Element(Cell).Text($"{line.DateStart:dd.MM.yyyy} - {line.DateEnd:dd.MM.yyyy}");
                        table.Cell().Element(Cell).AlignRight().Text($"{line.Duration}");
                        table.Cell().Element(Cell).AlignRight().Text($"{line.Copies}");
                        isFirst = false;
                    } // foreach line

                    table.Cell().ColumnSpan(4).Element(TotalCell).AlignRight()
                        .Text($"Итого по изданию {publication.Key}: средний срок и всего экземпляров");
                    table.Cell().Element(TotalCell).AlignRight()
                        .Text($"{publication.Average(line => line.Duration):n1}");
                    table.Cell().Element(TotalCell).AlignRight()
                        .Text($"{publication.Sum(line => line.Copies)}");
                } // foreach publication
            });

            column.Item().PaddingTop(4).Text($"Различных изданий на участке: {publications.Count}").Bold();
        });
    } // ComposeDistrict


    // итоги по отделению
    private void ComposeTotals(IContainer container) {
        container.BorderTop(1).PaddingTop(6).Column(column => {
            column.Item().Text("Итого по отделению").FontSize(11).Bold();
            column.Item().Text($"Работает почтальонов: {totals.Postmen}");
            column.Item().Text($"Участков обслуживает отделение: {totals.Districts}");
            column.Item().Text($"Различных изданий доставляется подписчикам: {totals.Publications}");
        });
    } // ComposeTotals


    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(3).DefaultTextStyle(text => text.Bold());


    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);


    private static IContainer TotalCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Darken1).Padding(3).DefaultTextStyle(text => text.Italic());

} // class DeliveryReportDocument
