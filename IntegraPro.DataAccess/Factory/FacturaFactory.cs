using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class FacturaFactory(string connectionString) : MasterDao(connectionString)
{
    public int CrearEncabezado(FacturaDTO factura, string consecutivo, string clave)
    {
        // Cambiamos el nombre del parámetro @desc a @tdesc para evitar conflictos con palabras reservadas
        string sql = @"INSERT INTO FACTURA_ENCABEZADO 
            (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, fecha, condicion_venta, medio_pago, total_neto, total_descuento, total_impuesto, total_comprobante, estado_hacienda)
            VALUES (@cli, @suc, @usu, @cons, @clav, GETDATE(), @cond, @medi, @neto, @tdesc, @imp, @total, 'Pendiente');
            SELECT CAST(SCOPE_IDENTITY() as int) AS NuevoID;";

        var p = new[] {
            new SqlParameter("@cli", SqlDbType.Int) { Value = factura.ClienteId },
            new SqlParameter("@suc", SqlDbType.Int) { Value = factura.SucursalId },
            new SqlParameter("@usu", SqlDbType.Int) { Value = factura.UsuarioId },
            new SqlParameter("@cons", SqlDbType.NVarChar) { Value = consecutivo },
            new SqlParameter("@clav", SqlDbType.NVarChar) { Value = clave },
            new SqlParameter("@cond", SqlDbType.NVarChar) { Value = factura.CondicionVenta },
            new SqlParameter("@medi", SqlDbType.NVarChar) { Value = factura.MedioPago },
            new SqlParameter("@neto", SqlDbType.Decimal) { Value = factura.TotalNeto },
            new SqlParameter("@tdesc", SqlDbType.Decimal) { Value = 0.0m }, // Aseguramos que siempre viaje como decimal
            new SqlParameter("@imp", SqlDbType.Decimal) { Value = factura.TotalImpuesto },
            new SqlParameter("@total", SqlDbType.Decimal) { Value = factura.TotalComprobante }
        };

        // Usamos ExecuteQuery indicando false para que no busque un Stored Procedure
        var dt = ExecuteQuery(sql, p, false);

        if (dt != null && dt.Rows.Count > 0)
        {
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        throw new Exception("No se pudo obtener el ID de la factura recién creada.");
    }

    public void InsertarDetalle(int facturaId, FacturaDetalleDTO d)
    {
        string sql = @"INSERT INTO FACTURA_DETALLE 
            (factura_id, producto_id, cantidad, precio_unitario, porcentaje_descuento, monto_descuento, monto_impuesto, total_linea)
            VALUES (@fid, @pid, @cant, @prec, @pdes, @mdes, @mimp, @total)";

        decimal montoDesc = (d.PrecioUnitario * d.Cantidad) * (d.PorcentajeDescuento / 100);
        decimal subtotal = (d.PrecioUnitario * d.Cantidad) - montoDesc;
        decimal impuesto = subtotal * 0.13m;

        var p = new[] {
            new SqlParameter("@fid", SqlDbType.Int) { Value = facturaId },
            new SqlParameter("@pid", SqlDbType.Int) { Value = d.ProductoId },
            new SqlParameter("@cant", SqlDbType.Decimal) { Value = d.Cantidad },
            new SqlParameter("@prec", SqlDbType.Decimal) { Value = d.PrecioUnitario },
            new SqlParameter("@pdes", SqlDbType.Decimal) { Value = d.PorcentajeDescuento },
            new SqlParameter("@mdes", SqlDbType.Decimal) { Value = montoDesc },
            new SqlParameter("@mimp", SqlDbType.Decimal) { Value = impuesto },
            new SqlParameter("@total", SqlDbType.Decimal) { Value = subtotal + impuesto }
        };

        ExecuteNonQuery(sql, p, false);
    }
}