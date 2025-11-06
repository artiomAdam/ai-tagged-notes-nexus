
using Microsoft.Data.Sqlite;

namespace Nexus.Core.Storage
{
    public class DbContext
    {
        private readonly string _dbPath;
        private readonly string _dbName = "nexus.db";

        public DbContext()
        {
            // make sure folder exists
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var appFolder = Path.Combine(documents, "KnowledgeNexus");
            Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, _dbName);
        }

        public void Initialize()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmdText = @"
                        CREATE TABLE IF NOT EXISTS Notes (
                            Id TEXT PRIMARY KEY,
                            Title TEXT
                            Content TEXT,
                            CreatedAt TEXT,
                            UpdatedAt TEXT );";

            using var command = connection.CreateCommand();
            command.CommandText = cmdText;
            command.ExecuteNonQuery();
            connection.Close();
        }

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            return connection;
        }
    }
}
