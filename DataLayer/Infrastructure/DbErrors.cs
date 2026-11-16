using Microsoft.Data.SqlClient;

namespace DataLayer.Infrastructure;

// Текст ошибки для пользователя. Триггер, отменивший изменение, присылает две ошибки:
// системную 3609 «транзакция завершилась в триггере» и нашу фразу с номером 50000,
// пользователю показывается только наша фраза
public static class DbErrors
{

    // фразы триггеров, если они есть, иначе текст исключения. Ошибка базы приходит
    // и вложенной: EF заворачивает её в DbUpdateException, загрузка окна - в
    // XamlParseException, поэтому показывается первопричина, а не обёртка
    public static string Text(Exception ex) {
        var cause = ex;
        while (cause is not SqlException && cause.InnerException != null) {
            cause = cause.InnerException;
        } // while

        if (cause is not SqlException sqlException) {
            return cause.Message;
        } // if

        List<string> messages = [.. sqlException.Errors
            .Cast<SqlError>()
            .Where(error => error.Number == 50000)
            .Select(error => error.Message)
        ];

        return messages.Count > 0
            ? string.Join("\n", messages)
            : sqlException.Message;
    } // Text

} // class DbErrors
