using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class AbonoFactory(string connectionString) : MasterDao(connectionString)
{
    public void ProcesarAbonoCompleto(AbonoDTO abono, int clienteId)
    {
        // Usamos una transacción para que si algo falla, no se descuente a medias
        string sql = @"
            BEGIN TRANSACTION;
            BEGIN TRY
                -- 1. Insertar el registro del abono
                INSERT INTO ABONO_CLIENTE (cuenta_cobrar_id, usuario_id, sucursal_id, fecha, monto_abonado, medio_pago, referencia)
                VALUES (@ccid, @uid, @sid, GETDATE(), @monto, @medio, @ref);

                -- 2. Restar el saldo de la factura específica (CUENTA_COBRAR)
                UPDATE CUENTA_COBRAR 
                SET saldo_pendiente = saldo_pendiente - @monto,
                    estado = CASE WHEN (saldo_pendiente - @monto) <= 0 THEN 'Pagada' ELSE 'Pendiente' END
                WHERE id = @ccid;

                -- 3. Restar el saldo general del CLIENTE (para liberar su crédito)
                UPDATE CLIENTE 
                SET saldo_pendiente = saldo_pendiente - @monto 
                WHERE id = @clienteId;

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION;
                THROW;
            END CATCH";

        var p = new[] {
            new SqlParameter("@ccid", abono.CuentaCobrarId),
            new SqlParameter("@uid", abono.UsuarioId),
            new SqlParameter("@sid", abono.SucursalId),
            new SqlParameter("@monto", abono.MontoAbonado),
            new SqlParameter("@medio", abono.MedioPago),
            new SqlParameter("@ref", (object?)abono.Referencia ?? DBNull.Value),
            new SqlParameter("@clienteId", clienteId)
        };

        ExecuteNonQuery(sql, p, false);
    }

    // --- NUEVO MÉTODO PARA CONSULTAR DEUDAS PENDIENTES ---
    public List<object> ObtenerFacturasPendientes(int? clienteId = null)
    {
        string sql = "SELECT * FROM VW_EstadoCuenta_Clientes";
        List<SqlParameter> parametros = new List<SqlParameter>();

        if (clienteId.HasValue)
        {
            sql += " WHERE cliente_id = @cid";
            parametros.Add(new SqlParameter("@cid", clienteId.Value));
        }

        var dt = ExecuteQuery(sql, parametros.Count > 0 ? parametros.ToArray() : null, false);

        var lista = new List<object>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new
            {
                ClienteId = row["cliente_id"],
                Cliente = row["cliente_nombre"].ToString(),
                IdCuenta = row["cuenta_id"],
                FacturaNumero = row["factura_numero"].ToString(),
                MontoTotal = row["monto_total"],
                SaldoPendiente = row["saldo_pendiente"],
                FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]).ToShortDateString(),
                DiasRestantes = row["dias_para_vencimiento"]
            });
        }
        return lista;
    }
}