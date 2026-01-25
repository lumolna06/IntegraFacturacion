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

    // 1. Para consultas SELECT
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

    // 2. ExecuteScalar (Devuelve un objeto, ej: un ID)
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

    // 3. CORREGIDO: Ahora devuelve INT (filas afectadas)
    protected int ExecuteNonQuery(string query, SqlParameter[]? parameters = null, bool isStoredProcedure = true)
    {
        using var connection = GetConnection();
        using var command = new SqlCommand(query, connection);
        command.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        connection.Open();
        return command.ExecuteNonQuery(); // Devuelve el número de filas
    }

    // 4. CORREGIDO: Ahora devuelve INT para compatibilidad
    protected int ExecuteStoredProcedure(string spName, SqlParameter[] parameters)
    {
        return ExecuteNonQuery(spName, parameters, true);
    }

    // ==========================================
    // NUEVOS MÉTODOS PARA TRANSACCIONES (CORREGIDOS)
    // ==========================================

    protected object ExecuteScalarInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        if (parameters != null) command.Parameters.AddRange(parameters);
        var result = command.ExecuteScalar();
        return result ?? DBNull.Value;
    }

    // CORREGIDO: Ahora devuelve INT
    protected int ExecuteNonQueryInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        if (parameters != null) command.Parameters.AddRange(parameters);
        return command.ExecuteNonQuery(); // Devuelve el número de filas
    }
}