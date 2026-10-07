using Microsoft.Data.Sqlite;

namespace LzyFlow.IntegrationTests.Fixtures;

public sealed class TemporarySqliteDatabase : IDisposable
{
    private readonly TemporaryDirectory _directory;

    public string DatabasePath { get; }

    public string ConnectionString { get; }

    public TemporarySqliteDatabase()
    {
        _directory = new TemporaryDirectory();

        DatabasePath = _directory.GetPath("lzyflow.db");

        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
        };

        ConnectionString = connectionStringBuilder.ToString();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        return connection;
    }

    public void Dispose()
    {
        _directory.Dispose();
    }
}