using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class AbonoFactory(string connectionString) : MasterDao(connectionString)
{
    // 1. REGISTRAR ABONO (Mantiene tu lógica transaccional)
    public void ProcesarAbonoCompleto(AbonoDTO abono, int clienteId)
    {
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

    // 2. BUSCAR CUENTAS POR COBRAR (Soporta: Cédula, Nombre, Correo, Teléfono, Factura)
    public List<CxcConsultaDTO> BuscarCxcClientes(string filtro)
    {
        // Usamos las columnas exactas de tu tabla CLIENTE (identificacion)
        string sql = @"SELECT * FROM VW_EstadoCuenta_Clientes 
                       WHERE cliente_nombre LIKE @f 
                          OR factura_numero LIKE @f 
                          OR cedula LIKE @f 
                          OR correo LIKE @f 
                          OR telefono LIKE @f
                       ORDER BY fecha_vencimiento ASC";

        var p = new[] { new SqlParameter("@f", $"%{filtro}%") };
        var dt = ExecuteQuery(sql, p, false);

        var lista = new List<CxcConsultaDTO>();
        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new CxcConsultaDTO
            {
                FacturaId = Convert.ToInt32(row["cuenta_id"]),
                NumeroFactura = row["factura_numero"].ToString()!,
                ClienteNombre = row["cliente_nombre"].ToString()!,
                ClienteCedula = row["cedula"]?.ToString() ?? "N/A",
                FechaEmision = Convert.ToDateTime(row["fecha_emision"]),
                FechaVencimiento = Convert.ToDateTime(row["fecha_vencimiento"]),
                MontoTotal = Convert.ToDecimal(row["monto_total"]),
                SaldoPendiente = Convert.ToDecimal(row["saldo_pendiente"]),
                Estado = "Pendiente",
                DiasCredito = Convert.ToInt32(row["dias_para_vencimiento"])
            });
        }
        return lista;
    }

    // 3. HISTORIAL DE ABONOS (Muestra quién recibió el dinero)
    public List<AbonoHistorialDTO> ListarHistorialAbonos(int facturaId)
    {
        string sql = @"SELECT A.id, A.monto_abonado, A.fecha, A.medio_pago, A.referencia, U.nombre_completo
                       FROM ABONO_CLIENTE A
                       INNER JOIN USUARIO U ON A.usuario_id = U.id
                       WHERE A.cuenta_cobrar_id = @fId
                       ORDER BY A.fecha DESC";

        var p = new[] { new SqlParameter("@fId", facturaId) };
        var dt = ExecuteQuery(sql, p, false);

        var lista = new List<AbonoHistorialDTO>();
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

    // 4. ALERTAS DE MORA (Facturas vencidas con saldo)
    public List<AlertaCxcDTO> ObtenerAlertasVencimiento()
    {
        string sql = @"SELECT cuenta_id, cliente_nombre, factura_numero, saldo_pendiente, fecha_vencimiento,
                       dias_para_vencimiento
                       FROM VW_EstadoCuenta_Clientes
                       WHERE saldo_pendiente > 0 AND fecha_vencimiento < GETDATE()";

        var dt = ExecuteQuery(sql, null, false);
        var alertas = new List<AlertaCxcDTO>();

        foreach (DataRow row in dt.Rows)
        {
            int dias = Convert.ToInt32(row["dias_para_vencimiento"]);
            alertas.Add(new AlertaCxcDTO
            {
                FacturaId = Convert.ToInt32(row["cuenta_id"]),
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

    // 5. RESUMEN GENERAL CXC (Totales para el Dashboard)
    public ResumenCxcDTO ObtenerTotalesCxc()
    {
        string sql = @"SELECT 
                        SUM(saldo_pendiente) as TotalCobrar,
                        SUM(CASE WHEN fecha_vencimiento < GETDATE() THEN saldo_pendiente ELSE 0 END) as TotalVencido,
                        COUNT(cuenta_id) as CantidadFacturas,
                        COUNT(DISTINCT cliente_id) as TotalClientes
                       FROM VW_EstadoCuenta_Clientes 
                       WHERE saldo_pendiente > 0";

        var dt = ExecuteQuery(sql, null, false);
        if (dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            return new ResumenCxcDTO
            {
                TotalPorCobrar = row["TotalCobrar"] != DBNull.Value ? Convert.ToDecimal(row["TotalCobrar"]) : 0,
                TotalVencido = row["TotalVencido"] != DBNull.Value ? Convert.ToDecimal(row["TotalVencido"]) : 0,
                CantidadFacturasPendientes = row["CantidadFacturas"] != DBNull.Value ? Convert.ToInt32(row["CantidadFacturas"]) : 0,
                ClientesMorosos = row["TotalClientes"] != DBNull.Value ? Convert.ToInt32(row["TotalClientes"]) : 0
            };
        }
        return new ResumenCxcDTO();
    }

    // 6. LISTADO SIMPLE DE PENDIENTES
    public List<object> ObtenerFacturasPendientes(int? clienteId = null)
    {
        string sql = "SELECT * FROM VW_EstadoCuenta_Clientes WHERE saldo_pendiente > 0";
        List<SqlParameter> parametros = new List<SqlParameter>();

        if (clienteId.HasValue)
        {
            sql += " AND cliente_id = @cid";
            parametros.Add(new SqlParameter("@cid", clienteId.Value));
        }

        var dt = ExecuteQuery(sql, (parametros.Count > 0 ? parametros.ToArray() : null), false);

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