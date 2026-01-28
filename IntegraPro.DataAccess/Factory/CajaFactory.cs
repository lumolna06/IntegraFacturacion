using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System;

namespace IntegraPro.DataAccess.Factory;

public class CajaFactory(string connectionString) : MasterDao(connectionString)
{
    public int AbrirCaja(CajaAperturaDTO apertura, UsuarioDTO ejecutor)
    {
        // 1. SEGURIDAD: Validar acceso y escritura
        ejecutor.ValidarAcceso("caja");
        ejecutor.ValidarEscritura();

        string sql = @"INSERT INTO CAJA_CIERRE (sucursal_id, usuario_id, fecha_apertura, monto_inicial, estado) 
                       VALUES (@sid, @uid, GETDATE(), @monto, 'Abierta');
                       SELECT CAST(SCOPE_IDENTITY() as int);";

        var p = new[] {
            new SqlParameter("@sid", ejecutor.SucursalId),
            new SqlParameter("@uid", ejecutor.Id),
            new SqlParameter("@monto", apertura.MontoInicial)
        };

        var dt = ExecuteQuery(sql, p, false);
        return Convert.ToInt32(dt.Rows[0][0]);
    }

    public void CerrarCaja(CajaCierreDTO cierre, UsuarioDTO ejecutor)
    {
        // 2. SEGURIDAD: Validar permisos de escritura antes de procesar
        ejecutor.ValidarAcceso("caja");
        ejecutor.ValidarEscritura();

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
        // 3. SEGURIDAD: Lógica de prioridad de permisos
        // Si el usuario tiene el permiso 'all', el filtro permanece vacío y ve todo.
        string sucursalFilter = "";
        List<SqlParameter> parametros = new List<SqlParameter>();

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            // Ahora que la vista tiene sucursal_id, podemos filtrar sin errores
            sucursalFilter = " WHERE sucursal_id = @sid";
            parametros.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        string sql = $@"SELECT * FROM VW_REPORTE_CIERRES_CAJA 
                        {sucursalFilter} 
                        ORDER BY fecha_cierre DESC";

        var dt = ExecuteQuery(sql, parametros.Count > 0 ? parametros.ToArray() : null, false);
        var lista = new List<object>();

        if (dt == null) return lista;

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new
            {
                Id = row["caja_id"],
                Sucursal = row["sucursal"].ToString(),
                Cajero = row["cajero"].ToString(),
                Apertura = row["fecha_apertura"],
                Cierre = row["fecha_cierre"],
                MontoInicial = row["monto_inicial"],
                VentasEfectivo = row["ventas_efectivo"],
                AbonosEfectivo = row["abonos_efectivo"],
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