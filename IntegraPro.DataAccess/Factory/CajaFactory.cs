using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CajaFactory(string connectionString) : MasterDao(connectionString)
{
    public int AbrirCaja(CajaAperturaDTO apertura)
    {
        string sql = @"INSERT INTO CAJA_CIERRE (sucursal_id, usuario_id, fecha_apertura, monto_inicial, estado) 
                       VALUES (@sid, @uid, GETDATE(), @monto, 'Abierta');
                       SELECT CAST(SCOPE_IDENTITY() as int);";

        var p = new[] {
            new SqlParameter("@sid", apertura.SucursalId),
            new SqlParameter("@uid", apertura.UsuarioId),
            new SqlParameter("@monto", apertura.MontoInicial)
        };
        var dt = ExecuteQuery(sql, p, false);
        return Convert.ToInt32(dt.Rows[0][0]);
    }

    public void CerrarCaja(CajaCierreDTO cierre)
    {
        string sqlInfo = "SELECT sucursal_id, fecha_apertura FROM CAJA_CIERRE WHERE id = @id AND estado = 'Abierta'";
        var dtInfo = ExecuteQuery(sqlInfo, new[] { new SqlParameter("@id", cierre.CajaId) }, false);

        if (dtInfo.Rows.Count == 0) throw new Exception("La caja no existe o ya se encuentra cerrada.");

        var sid = dtInfo.Rows[0]["sucursal_id"];
        var fechaApertura = dtInfo.Rows[0]["fecha_apertura"];

        string sqlCierre = @"
            DECLARE @ventas decimal(18,2) = (SELECT ISNULL(SUM(total_comprobante),0) FROM FACTURA_ENCABEZADO 
                                            WHERE sucursal_id = @sid AND fecha >= @fecha AND medio_pago = 'Efectivo');
            
            DECLARE @abonos decimal(18,2) = (SELECT ISNULL(SUM(monto_abonado),0) FROM ABONO_CLIENTE 
                                            WHERE sucursal_id = @sid AND fecha >= @fecha AND medio_pago = 'Efectivo');

            UPDATE CAJA_CIERRE SET 
                fecha_cierre = GETDATE(),
                ventas_efectivo = @ventas,
                abonos_efectivo = @abonos,
                esperado_caja = monto_inicial + @ventas + @abonos,
                real_caja = @montoReal,
                notas = @notas,
                estado = 'Cerrada'
            WHERE id = @id";

        var p = new[] {
            new SqlParameter("@id", cierre.CajaId),
            new SqlParameter("@sid", sid),
            new SqlParameter("@fecha", fechaApertura),
            new SqlParameter("@montoReal", cierre.MontoRealEnCaja),
            new SqlParameter("@notas", (object?)cierre.Notas ?? DBNull.Value)
        };

        ExecuteNonQuery(sqlCierre, p, false);
    }

    public List<object> ObtenerHistorialCierres()
    {
        string sql = "SELECT * FROM VW_REPORTE_CIERRES_CAJA ORDER BY fecha_cierre DESC";
        var dt = ExecuteQuery(sql, null, false);

        var lista = new List<object>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new
            {
                Id = row["caja_id"],
                Sucursal = row["sucursal"].ToString(),
                Cajero = row["cajero"].ToString(),
                Apertura = row["fecha_apertura"],
                Cierre = row["fecha_cierre"],
                Esperado = row["esperado_caja"],
                Real = row["real_caja"],
                Diferencia = row["diferencia"],
                Estado = row["estado_auditoria"],
                Notas = row["notas"].ToString()
            });
        }
        return lista;
    }

}