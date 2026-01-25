using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CajaFactory(string connectionString) : MasterDao(connectionString)
{
    public int AbrirCaja(CajaAperturaDTO apertura, UsuarioDTO ejecutor)
    {
        // 1. VALIDACIÓN: Evitar que cuentas de solo lectura operen la caja
        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su cuenta es de solo lectura. No puede abrir caja.");

        string sql = @"INSERT INTO CAJA_CIERRE (sucursal_id, usuario_id, fecha_apertura, monto_inicial, estado) 
                       VALUES (@sid, @uid, GETDATE(), @monto, 'Abierta');
                       SELECT CAST(SCOPE_IDENTITY() as int);";

        var p = new[] {
            new SqlParameter("@sid", ejecutor.SucursalId), // Usamos la sucursal del ejecutor por seguridad
            new SqlParameter("@uid", ejecutor.Id),         // El ejecutor es el responsable
            new SqlParameter("@monto", apertura.MontoInicial)
        };

        var dt = ExecuteQuery(sql, p, false);
        return Convert.ToInt32(dt.Rows[0][0]);
    }

    public void CerrarCaja(CajaCierreDTO cierre, UsuarioDTO ejecutor)
    {
        // 2. VALIDACIÓN: Seguridad de rol
        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("No tiene permisos para realizar cierres de caja.");

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

    public List<object> ObtenerHistorialCierres(UsuarioDTO ejecutor)
    {
        // 3. FILTRADO: Si no es admin, solo ve los cierres de su sucursal
        string sql = "SELECT * FROM VW_REPORTE_CIERRES_CAJA";

        // Si el rol no es administrador (supongamos Rol 1), filtramos por sucursal
        if (ejecutor.RolId != 1)
        {
            sql += " WHERE sucursal_id = @sid";
        }

        sql += " ORDER BY fecha_cierre DESC";

        var p = (ejecutor.RolId != 1)
                ? new[] { new SqlParameter("@sid", ejecutor.SucursalId) }
                : null;

        var dt = ExecuteQuery(sql, p, false);
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