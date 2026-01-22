using System.Data;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Dao;

public abstract class MasterDao
{
    private readonly string _connectionString;

    protected MasterDao(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected SqlConnection GetConnection() => new SqlConnection(_connectionString);

    // 1. Para consultas SELECT (devuelve tablas)
    protected DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null, bool isStoredProcedure = true)
    {
        var table = new DataTable();
        using var connection = GetConnection();
        using var command = new SqlCommand(query, connection);

        command.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(table);
        return table;
    }

    // 2. MODIFICADO: Ahora soporta Texto plano sin romper llamadas viejas
    protected object ExecuteScalar(string query, SqlParameter[]? parameters = null, bool isStoredProcedure = true)
    {
        using var connection = GetConnection();
        using var command = new SqlCommand(query, connection);

        command.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        connection.Open();
        var result = command.ExecuteScalar();
        return result ?? DBNull.Value;
    }

    // 3. Para cambios directos (UPDATE, DELETE, INSERT simples)
    protected void ExecuteNonQuery(string query, SqlParameter[]? parameters = null, bool isStoredProcedure = true)
    {
        using var connection = GetConnection();
        using var command = new SqlCommand(query, connection);

        command.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        connection.Open();
        command.ExecuteNonQuery();
    }

    // 4. Alias para compatibilidad con UsuarioFactory
    protected void ExecuteStoredProcedure(string spName, SqlParameter[] parameters)
    {
        ExecuteNonQuery(spName, parameters, true);
    }

    // ==========================================
    // NUEVOS MÉTODOS PARA TRANSACCIONES (ACID)
    // ==========================================

    protected object ExecuteScalarInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        // Por defecto en transacciones manuales solemos usar Texto, 
        // pero puedes añadir lógica de CommandType aquí si fuera necesario.
        if (parameters != null) command.Parameters.AddRange(parameters);
        var result = command.ExecuteScalar();
        return result ?? DBNull.Value;
    }

    protected void ExecuteNonQueryInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        if (parameters != null) command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }
}