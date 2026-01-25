using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ClienteFactory(string connectionString) : MasterDao(connectionString)
{
    // 1. OBTENER TODOS (Clientes globales)
    public List<ClienteDTO> ObtenerTodos(UsuarioDTO ejecutor)
    {
        // El ejecutor debe tener permiso para ver el módulo de clientes
        ejecutor.ValidarAcceso("clientes");

        string sql = "SELECT id, identificacion, nombre, correo, telefono, limite_credito, activo FROM CLIENTE";

        var dt = ExecuteQuery(sql, null, false);
        var lista = new List<ClienteDTO>();

        if (dt == null) return lista;

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new ClienteDTO
            {
                Id = (int)row["id"],
                Identificacion = row["identificacion"].ToString() ?? "",
                Nombre = row["nombre"].ToString() ?? "",
                Correo = row["correo"]?.ToString(),
                Telefono = row["telefono"]?.ToString(),
                LimiteCredito = Convert.ToDecimal(row["limite_credito"]),
                Activo = Convert.ToBoolean(row["activo"])
            });
        }
        return lista;
    }

    // 2. INSERTAR (Sin sucursal_id)
    public int Insertar(ClienteDTO cliente, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("clientes");
        ejecutor.ValidarEscritura();

        string sql = @"INSERT INTO CLIENTE (identificacion, nombre, correo, telefono, limite_credito, activo) 
                       VALUES (@ident, @nomb, @corr, @tele, @limi, @acti);
                       SELECT CAST(SCOPE_IDENTITY() as int) AS NuevoID;";

        var p = new[] {
            new SqlParameter("@ident", cliente.Identificacion),
            new SqlParameter("@nomb", cliente.Nombre),
            new SqlParameter("@corr", (object?)cliente.Correo ?? DBNull.Value),
            new SqlParameter("@tele", (object?)cliente.Telefono ?? DBNull.Value),
            new SqlParameter("@limi", cliente.LimiteCredito),
            new SqlParameter("@acti", cliente.Activo)
        };

        var dt = ExecuteQuery(sql, p, false);
        return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
    }

    // 3. ACTUALIZAR
    public bool Actualizar(ClienteDTO cliente, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("clientes");
        ejecutor.ValidarEscritura();

        string sql = @"UPDATE CLIENTE SET 
                    identificacion = CASE 
                        WHEN @ident <> 'string' AND ISNULL(@ident, '') <> '' THEN @ident 
                        ELSE identificacion END, 
                    nombre = CASE 
                        WHEN @nomb <> 'string' AND ISNULL(@nomb, '') <> '' THEN @nomb 
                        ELSE nombre END, 
                    correo = CASE 
                        WHEN @corr <> 'string' AND ISNULL(@corr, '') <> '' THEN @corr 
                        ELSE correo END, 
                    telefono = CASE 
                        WHEN @tele <> 'string' AND ISNULL(@tele, '') <> '' THEN @tele 
                        ELSE telefono END, 
                    limite_credito = CASE 
                        WHEN @limi > 0 THEN @limi 
                        ELSE limite_credito END, 
                    activo = @acti 
                   WHERE id = @id";

        var p = new[] {
            new SqlParameter("@id", cliente.Id),
            new SqlParameter("@ident", (object?)cliente.Identificacion ?? DBNull.Value),
            new SqlParameter("@nomb", (object?)cliente.Nombre ?? DBNull.Value),
            new SqlParameter("@corr", (object?)cliente.Correo ?? DBNull.Value),
            new SqlParameter("@tele", (object?)cliente.Telefono ?? DBNull.Value),
            new SqlParameter("@limi", cliente.LimiteCredito),
            new SqlParameter("@acti", cliente.Activo)
        };

        ExecuteNonQuery(sql, p, false);
        return true;
    }

    // 4. ESTADO DE CRÉDITO
    public (decimal saldo, decimal limite, bool activo) ObtenerEstadoCredito(int clienteId)
    {
        // Se asume que el campo en SQL es saldo_pendiente
        string sql = "SELECT ISNULL(saldo_pendiente, 0) as saldo_pendiente, limite_credito, activo FROM CLIENTE WHERE id = @id";
        var dt = ExecuteQuery(sql, new[] { new SqlParameter("@id", clienteId) }, false);

        if (dt != null && dt.Rows.Count > 0)
        {
            return (
                Convert.ToDecimal(dt.Rows[0]["saldo_pendiente"]),
                Convert.ToDecimal(dt.Rows[0]["limite_credito"]),
                Convert.ToBoolean(dt.Rows[0]["activo"])
            );
        }
        return (0, 0, false);
    }

    // 5. ACTUALIZAR SALDO
    public void ActualizarSaldo(int clienteId, decimal monto)
    {
        string sql = "UPDATE CLIENTE SET saldo_pendiente = ISNULL(saldo_pendiente, 0) + @monto WHERE id = @id";
        ExecuteNonQuery(sql, [
            new SqlParameter("@monto", monto),
            new SqlParameter("@id", clienteId)
        ], false);
    }

    // 6. BUSCAR POR IDENTIFICACIÓN (Sincronizado con Service)
    public ClienteDTO? ObtenerPorIdentificacion(string identificacion, UsuarioDTO ejecutor)
    {
        // Nota: No filtramos por sucursal_id porque me indicas que no existe en la tabla.
        string sql = "SELECT * FROM CLIENTE WHERE identificacion = @cedula";

        var dt = ExecuteQuery(sql, [new SqlParameter("@cedula", identificacion)], false);

        if (dt == null || dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ClienteDTO
        {
            Id = Convert.ToInt32(row["id"]),
            Identificacion = row["identificacion"].ToString() ?? "",
            Nombre = row["nombre"].ToString() ?? "",
            Correo = row["correo"]?.ToString() ?? "",
            Telefono = row["telefono"] != DBNull.Value ? row["telefono"].ToString() : "",
            LimiteCredito = row["limite_credito"] != DBNull.Value ? Convert.ToDecimal(row["limite_credito"]) : 0,
            Activo = row["activo"] != DBNull.Value && Convert.ToBoolean(row["activo"])
        };
    }
}