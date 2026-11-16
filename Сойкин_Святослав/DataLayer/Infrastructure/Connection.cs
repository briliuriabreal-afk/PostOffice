namespace DataLayer.Infrastructure;

// Строка подключения к базе почтового отделения - одна на EF Core и ADO.NET.
// База создаётся скриптами из папки Database\Scripts решения
public static class Connection
{
    // на машине разработчика сервер - экземпляр SQLEXPRESS;
    // для экземпляра по умолчанию - строка с Data Source=.
    public static string ConnectionString { get; } =
        // @"Data Source=.;" +
        @"Data Source=.\SQLEXPRESS;" +
        @"Database=PostOffice_Soykin_PE521;" +
        @"Integrated Security=True;" +
        @"Encrypt=False;" +
        @"Pooling=True;";

} // class Connection
