using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProformaFactory(string connectionString) : MasterDao(connectionString)
{
    public int CrearProforma(ProformaEncabezadoDTO p)
    {
        decimal totalGeneralCalculado = 0;

        foreach (var det in p.Detalles)
        {
            string sqlValidar = "SELECT costo_actual, existencia FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            if (dt.Rows.Count == 0)
                throw new Exception($"El producto con ID {det.ProductoId} no existe.");

            DataRow row = dt.Rows[0];
            decimal precioVigente = Convert.ToDecimal(row["costo_actual"]);
            decimal existenciaDisponible = Convert.ToDecimal(row["existencia"]);

            if (existenciaDisponible < det.Cantidad)
                throw new Exception($"Existencia insuficiente para el producto {det.ProductoId}. Disponible: {existenciaDisponible}, Solicitado: {det.Cantidad}");

            det.PrecioUnitario = precioVigente;
            det.TotalLineas = precioVigente * (decimal)det.Cantidad;
            totalGeneralCalculado += det.TotalLineas;
        }

        string sqlEnc = @"INSERT INTO PROFORMA_ENCABEZADO (cliente_id, sucursal_id, fecha, fecha_vencimiento, total, estado)
                          VALUES (@cid, @sid, GETDATE(), @fvenc, @total, 'Pendiente');
                          SELECT SCOPE_IDENTITY();";

        var parametrosEnc = new[] {
            new SqlParameter("@cid", (object)p.ClienteId),
            new SqlParameter("@sid", (object)p.SucursalId),
            new SqlParameter("@fvenc", (object)p.FechaVencimiento),
            new SqlParameter("@total", (object)totalGeneralCalculado)
        };

        object result = ExecuteScalar(sqlEnc, parametrosEnc, false);
        int idGenerado = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

        foreach (var det in p.Detalles)
        {
            string sqlDet = @"INSERT INTO PROFORMA_DETALLE (proforma_id, producto_id, cantidad, precio_unitario, total_linea)
                              VALUES (@pid, @prodid, @cant, @pre, @total)";

            var parametrosDetalle = new[] {
                new SqlParameter("@pid", (object)idGenerado),
                new SqlParameter("@prodid", (object)det.ProductoId),
                new SqlParameter("@cant", (object)det.Cantidad),
                new SqlParameter("@pre", (object)det.PrecioUnitario),
                new SqlParameter("@total", (object)det.TotalLineas)
            };

            ExecuteNonQuery(sqlDet, parametrosDetalle, false);
        }
        return idGenerado;
    }

    public void ActualizarProforma(ProformaEncabezadoDTO p)
    {
        decimal totalGeneralCalculado = 0;
        foreach (var det in p.Detalles)
        {
            string sqlValidar = "SELECT costo_actual, existencia FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            if (dt.Rows.Count == 0) throw new Exception($"Producto {det.ProductoId} no encontrado.");

            decimal precioVigente = Convert.ToDecimal(dt.Rows[0]["costo_actual"]);
            decimal existenciaDisponible = Convert.ToDecimal(dt.Rows[0]["existencia"]);

            if (existenciaDisponible < det.Cantidad)
                throw new Exception($"Existencia insuficiente para el producto {det.ProductoId}.");

            det.PrecioUnitario = precioVigente;
            det.TotalLineas = precioVigente * (decimal)det.Cantidad;
            totalGeneralCalculado += det.TotalLineas;
        }

        string sql = @"
        BEGIN TRANSACTION;
        BEGIN TRY
            UPDATE PROFORMA_ENCABEZADO 
            SET cliente_id = @cid, sucursal_id = @sid, fecha_vencimiento = @fvenc, total = @total
            WHERE id = @id AND estado = 'Pendiente';

            DELETE FROM PROFORMA_DETALLE WHERE proforma_id = @id;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH";

        var parametrosUpdate = new[] {
            new SqlParameter("@id", (object)p.Id),
            new SqlParameter("@cid", (object)p.ClienteId),
            new SqlParameter("@sid", (object)p.SucursalId),
            new SqlParameter("@fvenc", (object)p.FechaVencimiento),
            new SqlParameter("@total", (object)totalGeneralCalculado)
        };

        ExecuteNonQuery(sql, parametrosUpdate, false);

        foreach (var det in p.Detalles)
        {
            string sqlDet = @"INSERT INTO PROFORMA_DETALLE (proforma_id, producto_id, cantidad, precio_unitario, total_linea)
                              VALUES (@pid, @prodid, @cant, @pre, @total)";

            var parametrosDet = new[] {
                new SqlParameter("@pid", (object)p.Id),
                new SqlParameter("@prodid", (object)det.ProductoId),
                new SqlParameter("@cant", (object)det.Cantidad),
                new SqlParameter("@pre", (object)det.PrecioUnitario),
                new SqlParameter("@total", (object)det.TotalLineas)
            };

            ExecuteNonQuery(sqlDet, parametrosDet, false);
        }
    }

    public List<ProformaEncabezadoDTO> ListarProformas(string filtro = "")
    {
        string sqlEnc = @"SELECT P.*, C.nombre as ClienteNombre 
                          FROM PROFORMA_ENCABEZADO P
                          JOIN CLIENTE C ON P.cliente_id = C.id
                          WHERE C.nombre LIKE @f 
                             OR P.estado LIKE @f 
                             OR C.identificacion LIKE @f
                             OR CAST(P.id AS NVARCHAR) LIKE @f
                          ORDER BY P.fecha DESC";

        var parametros = new[] { new SqlParameter("@f", (object)$"%{filtro}%") };
        var dtEnc = ExecuteQuery(sqlEnc, parametros, false);

        var lista = new List<ProformaEncabezadoDTO>();
        foreach (DataRow r in dtEnc.Rows)
        {
            var proforma = MapearEncabezado(r);
            proforma.Detalles = ObtenerDetallesProforma(proforma.Id);
            lista.Add(proforma);
        }
        return lista;
    }

    public ProformaEncabezadoDTO ObtenerPorId(int id)
    {
        string sql = @"SELECT P.*, C.nombre as ClienteNombre 
                       FROM PROFORMA_ENCABEZADO P
                       JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE P.id = @id";

        var dt = ExecuteQuery(sql, new[] { new SqlParameter("@id", id) }, false);

        if (dt.Rows.Count == 0) return null;

        var proforma = MapearEncabezado(dt.Rows[0]);
        proforma.Detalles = ObtenerDetallesProforma(id);
        return proforma;
    }

    public void AnularProforma(int id)
    {
        string sql = "UPDATE PROFORMA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id AND estado = 'Pendiente'";
        var parametros = new[] { new SqlParameter("@id", id) };

        // Ejecución corregida sin retorno de int
        ExecuteNonQuery(sql, parametros, false);
    }

    public List<ProformaEncabezadoDTO> ListarPorCliente(int clienteId)
    {
        string sql = @"SELECT P.*, C.nombre as ClienteNombre 
                       FROM PROFORMA_ENCABEZADO P
                       JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE P.cliente_id = @cid
                       ORDER BY P.fecha DESC";

        var dtEnc = ExecuteQuery(sql, new[] { new SqlParameter("@cid", (object)clienteId) }, false);
        var lista = new List<ProformaEncabezadoDTO>();

        foreach (DataRow r in dtEnc.Rows)
        {
            var proforma = MapearEncabezado(r);
            proforma.Detalles = ObtenerDetallesProforma(proforma.Id);
            lista.Add(proforma);
        }
        return lista;
    }

    private List<ProformaDetalleDTO> ObtenerDetallesProforma(int proformaId)
    {
        var detalles = new List<ProformaDetalleDTO>();
        string sqlDet = @"SELECT D.*, P.nombre as ProductoNombre 
                          FROM PROFORMA_DETALLE D
                          JOIN PRODUCTO P ON D.producto_id = P.id
                          WHERE D.proforma_id = @pid";

        var dtDet = ExecuteQuery(sqlDet, new[] { new SqlParameter("@pid", proformaId) }, false);

        foreach (DataRow rd in dtDet.Rows)
        {
            detalles.Add(new ProformaDetalleDTO
            {
                ProductoId = Convert.ToInt32(rd["producto_id"]),
                ProductoNombre = rd["ProductoNombre"].ToString(),
                Cantidad = Convert.ToInt32(rd["cantidad"]),
                PrecioUnitario = Convert.ToDecimal(rd["precio_unitario"]),
                TotalLineas = Convert.ToDecimal(rd["total_linea"])
            });
        }
        return detalles;
    }

    private ProformaEncabezadoDTO MapearEncabezado(DataRow r)
    {
        return new ProformaEncabezadoDTO
        {
            Id = Convert.ToInt32(r["id"]),
            ClienteId = Convert.ToInt32(r["cliente_id"]),
            ClienteNombre = r["ClienteNombre"].ToString(),
            SucursalId = r.Table.Columns.Contains("sucursal_id") ? Convert.ToInt32(r["sucursal_id"]) : 0,
            Total = Convert.ToDecimal(r["total"]),
            Fecha = Convert.ToDateTime(r["fecha"]),
            FechaVencimiento = r.Table.Columns.Contains("fecha_vencimiento") ? Convert.ToDateTime(r["fecha_vencimiento"]) : DateTime.MinValue,
            Estado = r["estado"].ToString() ?? "Pendiente",
            Detalles = new List<ProformaDetalleDTO>()
        };
    }

    public string ConvertirAFactura(int proformaId, int usuarioId, string medioPago)
    {
        string sql = @"
        BEGIN TRANSACTION;
        BEGIN TRY
            DECLARE @clienteId INT, @sucursalId INT, @total DECIMAL(18,2), @facturaId INT;
            DECLARE @consecutivo NVARCHAR(50) = 'FAC-' + CAST(NEXT VALUE FOR Seq_Consecutivos AS NVARCHAR(20));

            SELECT @clienteId = cliente_id, @sucursalId = sucursal_id, @total = total 
            FROM PROFORMA_ENCABEZADO WHERE id = @profId;

            IF @clienteId IS NULL THROW 50000, 'La proforma no existe.', 1;

            INSERT INTO FACTURA_ENCABEZADO (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, 
                                          total_neto, total_impuesto, total_comprobante, medio_pago, 
                                          estado_hacienda, condicion_venta, fecha)
            VALUES (@clienteId, @sucursalId, @usuarioId, @consecutivo, '00000', 
                    @total, 0, @total, @medioPago, 'Aceptado', 'Crédito', GETDATE());
            
            SET @facturaId = SCOPE_IDENTITY();

            INSERT INTO FACTURA_DETALLE (factura_id, producto_id, cantidad, precio_unitario, monto_impuesto, total_linea)
            SELECT @facturaId, producto_id, cantidad, precio_unitario, 0, total_linea
            FROM PROFORMA_DETALLE WHERE proforma_id = @profId;

            UPDATE P 
            SET P.existencia = P.existencia - D.cantidad
            FROM PRODUCTO P
            INNER JOIN PROFORMA_DETALLE D ON P.id = D.producto_id
            WHERE D.proforma_id = @profId;

            INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, 
                                             documento_referencia, notas)
            SELECT producto_id, @usuarioId, GETDATE(), 'SALIDA', cantidad, 
                   @consecutivo, 'Convertido desde Proforma #' + CAST(@profId AS NVARCHAR(10))
            FROM PROFORMA_DETALLE WHERE proforma_id = @profId;

            UPDATE PROFORMA_ENCABEZADO SET estado = 'Facturada' WHERE id = @profId;

            COMMIT TRANSACTION;
            SELECT @consecutivo;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH";

        var parametros = new[] {
            new SqlParameter("@profId", (object)proformaId),
            new SqlParameter("@usuarioId", (object)usuarioId),
            new SqlParameter("@medioPago", (object)medioPago)
        };

        object finalResult = ExecuteScalar(sql, parametros, false);
        return (finalResult != null && finalResult != DBNull.Value) ? finalResult.ToString() : string.Empty;
    }
}