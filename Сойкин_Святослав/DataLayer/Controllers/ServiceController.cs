using System.Text;
using Microsoft.Data.SqlClient;

namespace DataLayer.Controllers;

// Служебная команда: вернуть данные базы к исходным после проверок приёма,
// увольнения и оформления. Выполняются те же скрипты, что создают базу:
// заполнение таблиц и создание триггеров
public class ServiceController(string connectionString)
{

    // повторное заполнение таблиц и создание триггеров скриптами из папки
    public void ResetData(string scriptsFolder) {
        string[] scripts = ["005_Tables_Populate.sql", "008_Triggers_Create.sql"];
        List<string> batches = [];
        foreach (var script in scripts) {
            batches.AddRange(Batches(Path.Combine(scriptsFolder, script)));
        } // foreach script

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // данные и триггеры возвращаются вместе; при ошибке Dispose отменяет транзакцию
        using var transaction = connection.BeginTransaction();
        foreach (var batch in batches) {
            using var command = new SqlCommand(batch, connection, transaction);
            command.ExecuteNonQuery();
        } // foreach batch

        transaction.Commit();
    } // ResetData


    // пакеты скрипта - части между строками go, как их выполняет SSMS
    private static List<string> Batches(string fileName) {
        List<string> batches = [];
        var batch = new StringBuilder();

        foreach (var line in File.ReadLines(fileName)) {
            if (line.Trim().Equals("go", StringComparison.OrdinalIgnoreCase)) {
                batches.Add(batch.ToString());
                batch.Clear();
            } else {
                batch.AppendLine(line);
            } // if
        } // foreach line

        batches.Add(batch.ToString());
        return [.. batches.Where(text => !string.IsNullOrWhiteSpace(text))];
    } // Batches

} // class ServiceController
