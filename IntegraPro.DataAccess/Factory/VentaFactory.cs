using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class VentaFactory(string connectionString) : MasterDao(connectionString)
{
    public string CrearFactura(FacturaDTO venta)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    decimal acumuladoNeto = 0;
                    decimal acumuladoImpuesto = 0;
                    var detallesProcesados = new List<FacturaDetalleDTO>();

                    // 1. PROCESAR Y CALCULAR CADA LÍNEA DESDE LA DB
                    foreach (var item in venta.Detalles)
                    {
                        using (var cmdProd = new SqlCommand("SELECT nombre, costo_actual, porcentaje_impuesto FROM PRODUCTO WHERE id = @pid", connection, transaction))
                        {
                            cmdProd.Parameters.AddWithValue("@pid", item.ProductoId);
                            using (var reader = cmdProd.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new Exception($"El producto con ID {item.ProductoId} no existe.");

                                decimal precioReal = reader.GetDecimal(reader.GetOrdinal("costo_actual"));
                                decimal porcentajeIvaReal = reader.GetDecimal(reader.GetOrdinal("porcentaje_impuesto"));

                                decimal montoNetoLinea = item.Cantidad * precioReal;
                                decimal montoIvaLinea = montoNetoLinea * (porcentajeIvaReal / 100);
                                decimal totalLinea = montoNetoLinea + montoIvaLinea;

                                detallesProcesados.Add(new FacturaDetalleDTO
                                {
                                    ProductoId = item.ProductoId,
                                    Cantidad = item.Cantidad,
                                    PrecioUnitario = precioReal,
                                    PorcentajeImpuesto = porcentajeIvaReal,
                                    MontoImpuesto = montoIvaLinea,
                                    TotalLinea = totalLinea
                                });

                                acumuladoNeto += montoNetoLinea;
                                acumuladoImpuesto += montoIvaLinea;
                            }
                        }
                    }

                    // FACTURACIÓN ELECTRÓNICA (HACIENDA)
                    string consecutivoHacienda = venta.Consecutivo;
                    string claveNumerica = venta.ClaveNumerica;

                    if (string.IsNullOrEmpty(claveNumerica))
                    {
                        string cedulaEmisor = "3101123456".PadLeft(12, '0');
                        string terminal = "00001";
                        string numeroDoc = DateTime.Now.Ticks.ToString().Substring(10).PadLeft(10, '0');
                        consecutivoHacienda = "001" + terminal + "01" + numeroDoc;
                        claveNumerica = GenerarClaveHacienda(cedulaEmisor, consecutivoHacienda);
                    }

                    string estadoFinalHacienda = venta.EstadoHacienda ?? "LOCAL";

                    // 2. INSERTAR ENCABEZADO
                    string sqlEnc = @"INSERT INTO FACTURA_ENCABEZADO 
                                     (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, fecha, 
                                      condicion_venta, medio_pago, total_neto, total_impuesto, total_comprobante, 
                                      estado_hacienda, es_offline) 
                                     VALUES (@cid, @sid, @uid, @cons, @clave, GETDATE(), 
                                             @cond, @medio, @neto, @iva, @total, @estado, @offline);
                                     SELECT CAST(SCOPE_IDENTITY() as int);";

                    var pEnc = new[] {
                        new SqlParameter("@cid", venta.ClienteId),
                        new SqlParameter("@sid", venta.SucursalId),
                        new SqlParameter("@uid", venta.UsuarioId),
                        new SqlParameter("@cons", consecutivoHacienda),
                        new SqlParameter("@clave", claveNumerica),
                        new SqlParameter("@cond", venta.CondicionVenta ?? "Contado"),
                        new SqlParameter("@medio", venta.MedioPago ?? "Efectivo"),
                        new SqlParameter("@neto", acumuladoNeto),
                        new SqlParameter("@iva", acumuladoImpuesto),
                        new SqlParameter("@total", acumuladoNeto + acumuladoImpuesto),
                        new SqlParameter("@estado", estadoFinalHacienda),
                        new SqlParameter("@offline", venta.EsOffline ? 1 : 0)
                    };

                    int facturaId = Convert.ToInt32(ExecuteScalarInTransaction(sqlEnc, pEnc, connection, transaction));

                    // 3. INSERTAR DETALLES Y ACTUALIZAR KARDEX
                    foreach (var det in detallesProcesados)
                    {
                        string sqlDet = @"INSERT INTO FACTURA_DETALLE 
                                          (factura_id, producto_id, cantidad, precio_unitario, monto_impuesto, total_linea, porcentaje_impuesto) 
                                          VALUES (@fid, @pid, @cant, @pre, @miva, @tot, @piva)";

                        ExecuteNonQueryInTransaction(sqlDet, [
                            new SqlParameter("@fid", facturaId),
                            new SqlParameter("@pid", det.ProductoId),
                            new SqlParameter("@cant", det.Cantidad),
                            new SqlParameter("@pre", det.PrecioUnitario),
                            new SqlParameter("@miva", det.MontoImpuesto),
                            new SqlParameter("@tot", det.TotalLinea),
                            new SqlParameter("@piva", det.PorcentajeImpuesto)
                        ], connection, transaction);

                        string sqlKardex = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                                           (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) 
                                           VALUES (@pid, @uid, GETDATE(), 'SALIDA', @cant, @ref, 'Venta Automática')";

                        ExecuteNonQueryInTransaction(sqlKardex, [
                            new SqlParameter("@pid", det.ProductoId),
                            new SqlParameter("@uid", venta.UsuarioId),
                            new SqlParameter("@cant", det.Cantidad),
                            new SqlParameter("@ref", consecutivoHacienda)
                        ], connection, transaction);
                    }

                    transaction.Commit();
                    return consecutivoHacienda;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Error en VentaFactory: " + ex.Message);
                }
            }
        }
    }

    private string GenerarClaveHacienda(string cedula, string consecutivo)
    {
        string pais = "506";
        string dia = DateTime.Now.Day.ToString("D2");
        string mes = DateTime.Now.Month.ToString("D2");
        string anio = DateTime.Now.ToString("yy");
        string situacion = "1";
        string codigoSeguridad = new Random().Next(10000000, 99999999).ToString();
        return pais + dia + mes + anio + cedula + consecutivo + situacion + codigoSeguridad;
    }

    public FacturaDTO? ObtenerPorId(int id)
    {
        string sql = @"SELECT f.*, 
                               ISNULL(c.nombre, 'CLIENTE CONTADO') as ClienteNombre, 
                               c.identificacion as ClienteIdentificacion 
                      FROM FACTURA_ENCABEZADO f 
                      LEFT JOIN CLIENTE c ON f.cliente_id = c.id 
                      WHERE f.id = @id";

        var dt = ExecuteQuery(sql, [new SqlParameter("@id", id)], false);
        if (dt == null || dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new FacturaDTO
        {
            Id = Convert.ToInt32(row["id"]),
            ClienteId = row["cliente_id"] != DBNull.Value ? Convert.ToInt32(row["cliente_id"]) : 0,
            ClienteNombre = row["ClienteNombre"]?.ToString(),
            ClienteIdentificacion = row["ClienteIdentificacion"]?.ToString() ?? "",
            Consecutivo = row["consecutivo"]?.ToString(),
            ClaveNumerica = row["clave_numerica"]?.ToString(),
            Fecha = Convert.ToDateTime(row["fecha"]),
            TotalNeto = Convert.ToDecimal(row["total_neto"]),
            TotalImpuesto = Convert.ToDecimal(row["total_impuesto"]),
            TotalComprobante = Convert.ToDecimal(row["total_comprobante"]),
            CondicionVenta = row["condicion_venta"]?.ToString() ?? "Contado",
            MedioPago = row["medio_pago"]?.ToString() ?? "Efectivo"
        };
    }

    public List<FacturaDetalleDTO> ListarDetalles(int facturaId)
    {
        string sql = @"SELECT d.*, p.nombre as ProductoNombre 
                       FROM FACTURA_DETALLE d
                       INNER JOIN PRODUCTO p ON d.producto_id = p.id
                       WHERE d.factura_id = @fid";

        var dt = ExecuteQuery(sql, [new SqlParameter("@fid", facturaId)], false);
        var lista = new List<FacturaDetalleDTO>();

        if (dt != null)
        {
            foreach (DataRow dr in dt.Rows)
            {
                lista.Add(new FacturaDetalleDTO
                {
                    ProductoId = Convert.ToInt32(dr["producto_id"]),
                    ProductoNombre = dr["ProductoNombre"]?.ToString() ?? "Producto Desconocido",
                    Cantidad = Convert.ToDecimal(dr["cantidad"]),
                    PrecioUnitario = Convert.ToDecimal(dr["precio_unitario"]),
                    TotalLinea = Convert.ToDecimal(dr["total_linea"]),
                    PorcentajeImpuesto = Convert.ToDecimal(dr["porcentaje_impuesto"]),
                    MontoImpuesto = Convert.ToDecimal(dr["monto_impuesto"])
                });
            }
        }
        return lista;
    }

    // MODIFICADO: Ahora incluye filtro por condicionVenta
    public DataTable ObtenerReporteVentas(DateTime? desde, DateTime? hasta, int? clienteId, int? sucursalId, string busqueda, string? condicionVenta = "")
    {
        string sql = "SELECT * FROM VW_REPORTE_VENTAS WHERE 1=1";
        List<SqlParameter> parametros = new List<SqlParameter>();

        if (desde.HasValue)
        {
            sql += " AND fecha >= @desde";
            parametros.Add(new SqlParameter("@desde", desde.Value));
        }
        if (hasta.HasValue)
        {
            sql += " AND fecha <= @hasta";
            parametros.Add(new SqlParameter("@hasta", hasta.Value.Date.AddDays(1).AddSeconds(-1)));
        }
        if (clienteId.HasValue)
        {
            sql += " AND cliente_id = @cId";
            parametros.Add(new SqlParameter("@cId", clienteId.Value));
        }
        if (sucursalId.HasValue)
        {
            sql += " AND sucursal_id = @sId";
            parametros.Add(new SqlParameter("@sId", sucursalId.Value));
        }

        // FILTRO DE CONDICIÓN DE VENTA
        if (!string.IsNullOrEmpty(condicionVenta))
        {
            sql += " AND condicion_venta = @condicion";
            parametros.Add(new SqlParameter("@condicion", condicionVenta));
        }

        if (!string.IsNullOrEmpty(busqueda))
        {
            sql += " AND (consecutivo LIKE @bus OR cliente_cedula LIKE @bus OR cliente_nombre LIKE @bus)";
            parametros.Add(new SqlParameter("@bus", "%" + busqueda + "%"));
        }

        sql += " ORDER BY fecha DESC";
        return ExecuteQuery(sql, parametros.ToArray(), false);
    }
}