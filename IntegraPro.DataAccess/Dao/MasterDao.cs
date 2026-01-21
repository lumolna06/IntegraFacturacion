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

        if (isStoredProcedure)
            command.CommandType = CommandType.StoredProcedure;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(table);
        return table;
    }

    // 2. Para INSERT que devuelven el ID generado
    protected int ExecuteScalar(string spName, SqlParameter[]? parameters = null)
    {
        using var connection = GetConnection();
        using var command = new SqlCommand(spName, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (parameters != null)
            command.Parameters.AddRange(parameters);

        connection.Open();
        var result = command.ExecuteScalar();
        return result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    // 3. Para cambios directos (UPDATE, DELETE, INSERT simples)
    protected void ExecuteNonQuery(string query, SqlParameter[]? parameters = null, bool isStoredProcedure = true)
    {
        using var connection = GetConnection();
        using var command = new SqlCommand(query, connection);

        if (isStoredProcedure)
            command.CommandType = CommandType.StoredProcedure;

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

    // Permite ejecutar un Scalar (como obtener el ID de factura) dentro de una transacción abierta
    protected object ExecuteScalarInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        if (parameters != null) command.Parameters.AddRange(parameters);
        var result = command.ExecuteScalar();
        return result ?? DBNull.Value;
    }

    // Permite ejecutar comandos (como el detalle o el kardex) dentro de una transacción abierta
    protected void ExecuteNonQueryInTransaction(string sql, SqlParameter[] parameters, SqlConnection connection, SqlTransaction transaction)
    {
        using var command = new SqlCommand(sql, connection, transaction);
        if (parameters != null) command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }
}