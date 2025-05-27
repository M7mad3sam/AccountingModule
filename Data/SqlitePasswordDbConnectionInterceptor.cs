using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace AspNetCoreMvcTemplate.Data;

public class SqlitePasswordDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly string _password;

    public SqlitePasswordDbConnectionInterceptor(string password)
    {
        _password = password;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var connectionStringBuilder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString)
        {
            Password = _password
        };
        
        sqliteConnection.ConnectionString = connectionStringBuilder.ToString();
        return result;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        var sqliteConnection = (SqliteConnection)connection;
        var connectionStringBuilder = new SqliteConnectionStringBuilder(sqliteConnection.ConnectionString)
        {
            Password = _password
        };
        
        sqliteConnection.ConnectionString = connectionStringBuilder.ToString();
        return result;
    }
}
