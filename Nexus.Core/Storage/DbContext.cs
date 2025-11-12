
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace Nexus.Core.Storage
{
    public class DbContext : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _dbName = "nexus.db";
        private SqliteConnection? _connection;

        private readonly string notesTable = @"
        CREATE TABLE IF NOT EXISTS Notes (
            Id TEXT PRIMARY KEY,
            Title TEXT,
            Content TEXT,
            CreatedAt TEXT,
            UpdatedAt TEXT,
            ParentId TEXT NULL REFERENCES Notes(Id)
        );";


        private readonly string topicsTable = @"
        CREATE TABLE IF NOT EXISTS Topics (
            Id TEXT PRIMARY KEY,
            Name TEXT UNIQUE,
            Embedding BLOB
        );";

        private readonly string noteTopicsTable = @"
        CREATE TABLE IF NOT EXISTS NoteTopics (
            NoteId TEXT REFERENCES Notes(Id),
            TopicId TEXT REFERENCES Topics(Id),
            PRIMARY KEY (NoteId, TopicId)
        );";

        public DbContext(string? customPath = null)
        {   
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

            string cmdText = notesTable + topicsTable + noteTopicsTable;

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
