
using Microsoft.Data.Sqlite;

namespace Nexus.Core.Storage
{
    public class DbContext : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _dbName = "nexus.db";
        private SqliteConnection? _connection;

        public DbContext(string? customPath = null)
        {
            // make sure folder exists
            if (!string.IsNullOrEmpty(customPath))
            {
                _dbPath = customPath;
            }
            else
            {
                var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var appFolder = Path.Combine(documents, "KnowledgeNexus");
                Directory.CreateDirectory(appFolder);
                _dbPath = Path.Combine(appFolder, _dbName);
            }
        }

        public void Initialize()
        {
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            var cmdText = @"
                        CREATE TABLE IF NOT EXISTS Notes (
                            Id TEXT PRIMARY KEY,
                            Title TEXT,
                            Content TEXT,
                            CreatedAt TEXT,
                            UpdatedAt TEXT );";

            using var command = _connection.CreateCommand();
            command.CommandText = cmdText;
            command.ExecuteNonQuery();
            _connection.Close();
        }

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            return connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
