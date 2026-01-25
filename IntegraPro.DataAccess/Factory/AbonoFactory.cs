using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class AbonoFactory(string connectionString) : MasterDao(connectionString)
{
    // 1. REGISTRAR ABONO (Transaccional + Seguridad)
    public void ProcesarAbonoCompleto(AbonoDTO abono, int clienteId, UsuarioDTO ejecutor)
    {
        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Permisos insuficientes para registrar abonos.");

        string sql = @"
            BEGIN TRANSACTION;
            BEGIN TRY
                -- 1. Insertar el registro del abono
                INSERT INTO ABONO_CLIENTE (cuenta_cobrar_id, usuario_id, sucursal_id, fecha, monto_abonado, medio_pago, referencia)
                VALUES (@ccid, @uid, @sid, GETDATE(), @monto, @medio, @ref);

                -- 2. Restar el saldo de la cuenta por cobrar y actualizar estado
                UPDATE CUENTA_COBRAR 
                SET saldo_pendiente = saldo_pendiente - @monto,
                    estado = CASE WHEN (saldo_pendiente - @monto) <= 0.01 THEN 'Pagada' ELSE 'Pendiente' END
                WHERE id = @ccid;

                -- 3. Restar el saldo general del CLIENTE
                UPDATE CLIENTE 
                SET saldo_pendiente = saldo_pendiente - @monto 
                WHERE id = @clienteId;

                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH";

        var p = new[] {
            new SqlParameter("@ccid", abono.CuentaCobrarId),
            new SqlParameter("@uid", ejecutor.Id),
            new SqlParameter("@sid", ejecutor.SucursalId),
            new SqlParameter("@monto", abono.MontoAbonado),
            new SqlParameter("@medio", abono.MedioPago),
            new SqlParameter("@ref", (object?)abono.Referencia ?? DBNull.Value),
            new SqlParameter("@clienteId", clienteId)
        };

        ExecuteNonQuery(sql, p, false);
    }

    // 2. BUSCAR CUENTAS (Usa cuenta_id para FacturaId en el DTO)
    public List<CxcConsultaDTO> BuscarCxcClientes(string filtro, UsuarioDTO ejecutor)
    {
        // Usamos 1=1 para poder concatenar filtros opcionales dinámicamente
        string sql = @"SELECT * FROM VW_EstadoCuenta_Clientes WHERE 1=1 ";

        // Si el filtro tiene texto, aplicamos la búsqueda por campos
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            sql += " AND (cliente_nombre LIKE @f OR factura_numero LIKE @f OR cedula LIKE @f) ";
        }

        // Filtro de seguridad por sucursal
        if (ejecutor.RolId != 1)
            sql += " AND sucursal_id = @sid ";

        sql += " ORDER BY fecha_vencimiento ASC";

        var p = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(filtro))
            p.Add(new SqlParameter("@f", $"%{filtro}%"));

        if (ejecutor.RolId != 1)
            p.Add(new SqlParameter("@sid", ejecutor.SucursalId));

        var dt = ExecuteQuery(sql, p.ToArray(), false);
        var lista = new List<CxcConsultaDTO>();

        if (dt == null) return lista;

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new CxcConsultaDTO
            {
                // Mapeo corregido a la nueva propiedad del DTO
                CuentaCobrarId = (int)row["cuenta_id"],
                NumeroFactura = row["factura_numero"].ToString()!,
                ClienteNombre = row["cliente_nombre"].ToString()!,
                ClienteCedula = row["cedula"]?.ToString() ?? "N/A",
                FechaEmision = Convert.ToDateTime(row["fecha_emision"]),
                FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]),
                MontoTotal = Convert.ToDecimal(row["monto_total"]),
                SaldoPendiente = Convert.ToDecimal(row["saldo_pendiente"]),
                Estado = row["estado"]?.ToString() ?? "Pendiente",
                DiasCredito = Convert.ToInt32(row["dias_para_vencimiento"])
            });
        }
        return lista;
    }

    // 3. ALERTAS DE MORA
    public List<AlertaCxcDTO> ObtenerAlertasVencimiento(UsuarioDTO ejecutor)
    {
        string sql = @"SELECT * FROM VW_EstadoCuenta_Clientes 
                       WHERE saldo_pendiente > 0 AND fecha_vencimiento < GETDATE()";

        if (ejecutor.RolId != 1)
            sql += " AND sucursal_id = @sid";

        var p = (ejecutor.RolId != 1) ? new[] { new SqlParameter("@sid", ejecutor.SucursalId) } : null;
        var dt = ExecuteQuery(sql, p, false);

        var alertas = new List<AlertaCxcDTO>();
        if (dt == null) return alertas;

        foreach (DataRow row in dt.Rows)
        {
            int dias = Convert.ToInt32(row["dias_para_vencimiento"]);
            alertas.Add(new AlertaCxcDTO
            {
                FacturaId = (int)row["cuenta_id"],
                ClienteNombre = row["cliente_nombre"].ToString()!,
                NumeroFactura = row["factura_numero"].ToString()!,
                SaldoPendiente = Convert.ToDecimal(row["saldo_pendiente"]),
                FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]),
                DiasVencidos = Math.Abs(dias),
                NivelRiesgo = Math.Abs(dias) > 30 ? "Crítico" : "Mora"
            });
        }
        return alertas;
    }

    // 4. RESUMEN GENERAL
    public ResumenCxcDTO ObtenerTotalesCxc(UsuarioDTO ejecutor)
    {
        string sql = @"SELECT 
                        SUM(saldo_pendiente) as TotalCobrar,
                        SUM(CASE WHEN fecha_vencimiento < GETDATE() THEN saldo_pendiente ELSE 0 END) as TotalVencido,
                        COUNT(cuenta_id) as CantidadFacturas,
                        COUNT(DISTINCT cliente_id) as TotalClientes
                       FROM VW_EstadoCuenta_Clientes 
                       WHERE saldo_pendiente > 0";

        if (ejecutor.RolId != 1)
            sql += " AND sucursal_id = @sid";

        var p = (ejecutor.RolId != 1) ? new[] { new SqlParameter("@sid", ejecutor.SucursalId) } : null;
        var dt = ExecuteQuery(sql, p, false);

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["TotalCobrar"] != DBNull.Value)
        {
            var row = dt.Rows[0];
            return new ResumenCxcDTO
            {
                TotalPorCobrar = Convert.ToDecimal(row["TotalCobrar"]),
                TotalVencido = Convert.ToDecimal(row["TotalVencido"]),
                CantidadFacturasPendientes = Convert.ToInt32(row["CantidadFacturas"]),
                ClientesMorosos = Convert.ToInt32(row["TotalClientes"])
            };
        }
        return new ResumenCxcDTO();
    }

    // 5. HISTORIAL DE ABONOS (Muestra info aunque la cuenta esté en 0)
    public List<AbonoHistorialDTO> ListarHistorialAbonos(int cuentaId)
    {
        // Aquí cuentaId recibe el ID de la Cuenta por Cobrar
        string sql = @"SELECT A.id, A.monto_abonado, A.fecha, A.medio_pago, A.referencia, U.nombre_completo
                   FROM ABONO_CLIENTE A
                   INNER JOIN USUARIO U ON A.usuario_id = U.id
                   WHERE A.cuenta_cobrar_id = @cId
                   ORDER BY A.fecha DESC";

        var p = new[] { new SqlParameter("@cId", cuentaId) };
        var dt = ExecuteQuery(sql, p, false);

        var lista = new List<AbonoHistorialDTO>();
        if (dt == null) return lista;

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new AbonoHistorialDTO
            {
                Id = Convert.ToInt32(row["id"]),
                Monto = Convert.ToDecimal(row["monto_abonado"]),
                FechaAbono = Convert.ToDateTime(row["fecha"]),
                MetodoPago = row["medio_pago"].ToString()!,
                NumeroReferencia = row["referencia"]?.ToString() ?? "Sin Ref.",
                NombreUsuario = row["nombre_completo"].ToString()!
            });
        }
        return lista;
    }
}