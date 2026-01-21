using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class VentaFactory(string connectionString) : MasterDao(connectionString)
{
    public string CrearFactura(FacturaDTO venta)
    {
        string consecutivo = venta.Consecutivo ?? "LOC-" + DateTime.Now.Ticks.ToString().Substring(10);
        bool esOffline = string.IsNullOrEmpty(venta.ClaveNumerica);
        string claveNumerica = !esOffline ? venta.ClaveNumerica! : "999" + DateTime.Now.ToString("ddMMyy") + "01" + DateTime.Now.Ticks.ToString().Substring(0, 10).PadLeft(20, '0');

        if (claveNumerica.Length > 50) claveNumerica = claveNumerica.Substring(0, 50);

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // 1. Encabezado
                    string sqlEnc = "INSERT INTO FACTURA_ENCABEZADO (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, fecha, condicion_venta, medio_pago, total_neto, total_impuesto, total_comprobante, estado_hacienda, es_offline) " +
                                   "VALUES (@cid, @sid, @uid, @cons, @clave, GETDATE(), @cond, @medio, @neto, @iva, @total, @estado, @off); " +
                                   "SELECT CAST(SCOPE_IDENTITY() as int);";

                    var pEnc = new[] {
                        new SqlParameter("@cid", venta.ClienteId),
                        new SqlParameter("@sid", venta.SucursalId),
                        new SqlParameter("@uid", venta.UsuarioId),
                        new SqlParameter("@cons", consecutivo),
                        new SqlParameter("@clave", claveNumerica),
                        new SqlParameter("@cond", venta.CondicionVenta),
                        new SqlParameter("@medio", venta.MedioPago),
                        new SqlParameter("@neto", venta.TotalNeto),
                        new SqlParameter("@iva", venta.TotalImpuesto),
                        new SqlParameter("@total", venta.TotalComprobante),
                        new SqlParameter("@estado", esOffline ? "LOCAL" : "ACEPTADO"),
                        new SqlParameter("@off", esOffline)
                    };

                    int facturaId = Convert.ToInt32(ExecuteScalarInTransaction(sqlEnc, pEnc, connection, transaction));

                    // 2. Detalles
                    foreach (var det in venta.Detalles)
                    {
                        decimal mImpuesto = (det.Cantidad * det.PrecioUnitario) * (det.PorcentajeImpuesto / 100);
                        decimal tLinea = (det.Cantidad * det.PrecioUnitario) + mImpuesto;

                        string sqlDet = "INSERT INTO FACTURA_DETALLE (factura_id, producto_id, cantidad, precio_unitario, monto_impuesto, total_linea, porcentaje_descuento, monto_descuento) " +
                                       "VALUES (@fid, @pid, @cant, @pre, @miva, @tot, 0, 0)";

                        var pDet = new[] {
                            new SqlParameter("@fid", facturaId),
                            new SqlParameter("@pid", det.ProductoId),
                            new SqlParameter("@cant", det.Cantidad),
                            new SqlParameter("@pre", det.PrecioUnitario),
                            new SqlParameter("@miva", mImpuesto),
                            new SqlParameter("@tot", tLinea)
                        };
                        ExecuteNonQueryInTransaction(sqlDet, pDet, connection, transaction);

                        // 3. Kardex
                        string sqlKardex = "INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) " +
                                          "VALUES (@pid, @uid, GETDATE(), 'SALIDA', @cant, @ref, 'Venta')";

                        var pKar = new[] {
                            new SqlParameter("@pid", det.ProductoId),
                            new SqlParameter("@uid", venta.UsuarioId),
                            new SqlParameter("@cant", det.Cantidad),
                            new SqlParameter("@ref", consecutivo)
                        };
                        ExecuteNonQueryInTransaction(sqlKardex, pKar, connection, transaction);
                    }

                    transaction.Commit();
                    return consecutivo;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    throw new Exception("Error en VentaFactory: " + ex.Message);
                }
            }
        }
    }
}