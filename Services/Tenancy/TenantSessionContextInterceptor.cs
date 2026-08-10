using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Manage_KPI_or_OKR_System.Services.Tenancy;

/// <summary>
/// Copies the scoped application tenant boundary into each logical SQL connection.
/// SQL Server RLS consumes these read-only values as a defense-in-depth layer.
/// </summary>
internal sealed class TenantSessionContextInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantSessionContextInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is SqlConnection)
        {
            using var command = CreateCommand(connection);
            command.ExecuteNonQuery();
        }
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is SqlConnection)
        {
            await using var command = CreateCommand(connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            """
            EXEC sys.sp_set_session_context @key=N'TenantId', @value=@tenantId, @read_only=1;
            EXEC sys.sp_set_session_context @key=N'SystemUserId', @value=@systemUserId, @read_only=1;
            """;

        AddParameter(command, "@tenantId", _tenantContext.TenantId ?? -1);
        AddParameter(command, "@systemUserId", _tenantContext.SystemUserId ?? -1);
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
