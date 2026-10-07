using LzyFlow.IntegrationTests.Fixtures;

namespace LzyFlow.IntegrationTests;

public sealed class TemporarySqliteDatabaseTests
{
    [Fact]
    public void OpenConnection_CreatesTemporaryDatabase()
    {
        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();

        Assert.Equal(
            System.Data.ConnectionState.Open,
            connection.State);

        Assert.True(File.Exists(database.DatabasePath));
    }

    [Fact]
    public void Database_CanPersistAndReadData()
    {
        using var database = new TemporarySqliteDatabase();
        using var connection = database.OpenConnection();

        using var createCommand = connection.CreateCommand();
        createCommand.CommandText =
            """
            CREATE TABLE test_items (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL
            );
            """;

        createCommand.ExecuteNonQuery();

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText =
            """
            INSERT INTO test_items (name)
            VALUES ('example');
            """;

        insertCommand.ExecuteNonQuery();

        using var queryCommand = connection.CreateCommand();
        queryCommand.CommandText =
            """
            SELECT name
            FROM test_items
            WHERE id = 1;
            """;

        var result = queryCommand.ExecuteScalar();

        Assert.Equal("example", result);
    }

    [Fact]
    public void Dispose_RemovesTemporaryDatabaseDirectory()
    {
        var database = new TemporarySqliteDatabase();
        var databasePath = database.DatabasePath;
        var directoryPath = System.IO.Path.GetDirectoryName(databasePath)!;

        using (var connection = database.OpenConnection())
        {
            Assert.True(File.Exists(databasePath));
        }

        database.Dispose();

        Assert.False(Directory.Exists(directoryPath));
    }
}