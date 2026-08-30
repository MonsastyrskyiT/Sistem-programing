using Microsoft.Data.Sqlite;

namespace ParalerSistem2;

internal sealed class SqliteUserRepository
{
    private readonly string _connectionString;

    public SqliteUserRepository(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath };
        _connectionString = builder.ToString();
        CreateTable();
    }

    public List<UserRecord> LoadAll()
    {
        var users = new List<UserRecord>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT FullName, Username FROM Users ORDER BY Id;";
        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
            users.Add(new UserRecord(reader.GetString(0), reader.GetString(1)));

        return users;
    }

    public int Import(IEnumerable<UserRecord> users)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            "INSERT OR IGNORE INTO Users (FullName, Username) VALUES ($fullName, $username);";
        SqliteParameter fullNameParameter = command.Parameters.Add("$fullName", SqliteType.Text);
        SqliteParameter usernameParameter = command.Parameters.Add("$username", SqliteType.Text);

        int inserted = 0;
        foreach (UserRecord user in users)
        {
            fullNameParameter.Value = user.FullName;
            usernameParameter.Value = user.Username;
            inserted += command.ExecuteNonQuery();
        }

        transaction.Commit();
        return inserted;
    }

    private void CreateTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users
            (
                Id       INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                Username TEXT NOT NULL UNIQUE
            );
            """;
        command.ExecuteNonQuery();
    }
}
