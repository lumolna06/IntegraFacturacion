using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProformaFactory(string connectionString) : MasterDao(connectionString)
{
    private readonly string _connectionString = connectionString;

    public int CrearProforma(ProformaEncabezadoDTO p)
    {
        var configFact = new ConfiguracionFactory(_connectionString);
        var empresa = configFact.ObtenerEmpresa();
        bool esTradicional = empresa == null || empresa.TipoRegimen.Equals("Tradicional", StringComparison.OrdinalIgnoreCase);

        decimal totalNetoAcumulado = 0;
        decimal totalIVAAcumulado = 0;

        // 1. Validar productos y calcular impuestos detallados
        foreach (var det in p.Detalles)
        {
            string sqlValidar = "SELECT costo_actual, existencia, porcentaje_impuesto FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            if (dt.Rows.Count == 0) throw new Exception($"El producto {det.ProductoId} no existe.");

            DataRow row = dt.Rows[0];
            decimal precioVigente = Convert.ToDecimal(row["costo_actual"]);
            decimal existenciaDisponible = Convert.ToDecimal(row["existencia"]);
            decimal pctIVA = Convert.ToDecimal(row["porcentaje_impuesto"]);

            if (existenciaDisponible < det.Cantidad)
                throw new Exception($"Stock insuficiente para {det.ProductoId}.");

            decimal subtotalLinea = precioVigente * (decimal)det.Cantidad;
            decimal impuestoLinea = esTradicional ? (subtotalLinea * (pctIVA / 100)) : 0;

            // Llenamos el DTO con el desglose real
            det.PrecioUnitario = precioVigente;
            det.PorcentajeImpuesto = esTradicional ? pctIVA : 0;
            det.ImpuestoTotal = impuestoLinea;
            det.TotalLineas = subtotalLinea + impuestoLinea;

            totalNetoAcumulado += subtotalLinea;
            totalIVAAcumulado += impuestoLinea;
        }

        // 2. Insertar Encabezado
        string sqlEnc = @"INSERT INTO PROFORMA_ENCABEZADO 
                          (cliente_id, sucursal_id, fecha, fecha_vencimiento, total_neto, total_impuesto, total, estado)
                          VALUES (@cid, @sid, GETDATE(), @fvenc, @tneto, @tiva, @total, 'Pendiente');
                          SELECT SCOPE_IDENTITY();";

        var parametrosEnc = new[] {
            new SqlParameter("@cid", p.ClienteId),
            new SqlParameter("@sid", p.SucursalId),
            new SqlParameter("@fvenc", p.FechaVencimiento),
            new SqlParameter("@tneto", totalNetoAcumulado),
            new SqlParameter("@tiva", totalIVAAcumulado),
            new SqlParameter("@total", totalNetoAcumulado + totalIVAAcumulado)
        };

        int idGenerado = Convert.ToInt32(ExecuteScalar(sqlEnc, parametrosEnc, false));

        // 3. Insertar Detalles con nuevas columnas SQL
        foreach (var det in p.Detalles)
        {
            string sqlDet = @"INSERT INTO PROFORMA_DETALLE 
                              (proforma_id, producto_id, cantidad, precio_unitario, porcentaje_impuesto, monto_impuesto, total_linea)
                              VALUES (@pid, @prodid, @cant, @pre, @pct, @montoIva, @total)";

            ExecuteNonQuery(sqlDet, new[] {
                new SqlParameter("@pid", idGenerado),
                new SqlParameter("@prodid", det.ProductoId),
                new SqlParameter("@cant", det.Cantidad),
                new SqlParameter("@pre", det.PrecioUnitario),
                new SqlParameter("@pct", det.PorcentajeImpuesto),
                new SqlParameter("@montoIva", det.ImpuestoTotal),
                new SqlParameter("@total", det.TotalLineas)
            }, false);
        }
        return idGenerado;
    }

    public void ActualizarProforma(ProformaEncabezadoDTO p)
    {
        var configFact = new ConfiguracionFactory(_connectionString);
        var empresa = configFact.ObtenerEmpresa();
        bool esTradicional = empresa == null || empresa.TipoRegimen.Equals("Tradicional", StringComparison.OrdinalIgnoreCase);

        var proformaActual = ObtenerPorId(p.Id);
        if (proformaActual == null) throw new Exception("La proforma no existe.");
        if (proformaActual.Estado != "Pendiente") throw new Exception("No se puede modificar una proforma ya procesada.");

        decimal totalNetoAcumulado = 0;
        decimal totalIVAAcumulado = 0;

        foreach (var det in p.Detalles)
        {
            string sqlValidar = "SELECT costo_actual, porcentaje_impuesto FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            decimal precioVigente = Convert.ToDecimal(dt.Rows[0]["costo_actual"]);
            decimal pctIVA = Convert.ToDecimal(dt.Rows[0]["porcentaje_impuesto"]);

            decimal subtotalLinea = precioVigente * (decimal)det.Cantidad;
            decimal impuestoLinea = esTradicional ? (subtotalLinea * (pctIVA / 100)) : 0;

            det.PrecioUnitario = precioVigente;
            det.PorcentajeImpuesto = esTradicional ? pctIVA : 0;
            det.ImpuestoTotal = impuestoLinea;
            det.TotalLineas = subtotalLinea + impuestoLinea;

            totalNetoAcumulado += subtotalLinea;
            totalIVAAcumulado += impuestoLinea;
        }

        string sql = @"
        BEGIN TRANSACTION;
        BEGIN TRY
            UPDATE PROFORMA_ENCABEZADO 
            SET cliente_id = @cid, sucursal_id = @sid, fecha_vencimiento = @fvenc, 
                total_neto = @tneto, total_impuesto = @tiva, total = @total, estado = @est
            WHERE id = @id;

            DELETE FROM PROFORMA_DETALLE WHERE proforma_id = @id;

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            THROW;
        END CATCH";

        ExecuteNonQuery(sql, new[] {
            new SqlParameter("@id", p.Id),
            new SqlParameter("@cid", p.ClienteId),
            new SqlParameter("@sid", p.SucursalId),
            new SqlParameter("@fvenc", p.FechaVencimiento),
            new SqlParameter("@tneto", totalNetoAcumulado),
            new SqlParameter("@tiva", totalIVAAcumulado),
            new SqlParameter("@total", totalNetoAcumulado + totalIVAAcumulado),
            new SqlParameter("@est", p.Estado)
        }, false);

        foreach (var det in p.Detalles)
        {
            string sqlDet = @"INSERT INTO PROFORMA_DETALLE (proforma_id, producto_id, cantidad, precio_unitario, porcentaje_impuesto, monto_impuesto, total_linea)
                              VALUES (@pid, @prodid, @cant, @pre, @pct, @montoIva, @total)";

            ExecuteNonQuery(sqlDet, new[] {
                new SqlParameter("@pid", p.Id),
                new SqlParameter("@prodid", det.ProductoId),
                new SqlParameter("@cant", det.Cantidad),
                new SqlParameter("@pre", det.PrecioUnitario),
                new SqlParameter("@pct", det.PorcentajeImpuesto),
                new SqlParameter("@montoIva", det.ImpuestoTotal),
                new SqlParameter("@total", det.TotalLineas)
            }, false);
        }
    }

    public List<ProformaEncabezadoDTO> ListarProformas(string filtro = "")
    {
        string sqlEnc = @"SELECT P.*, C.nombre as ClienteNombre, C.identificacion as ClienteIdentificacion 
                          FROM PROFORMA_ENCABEZADO P
                          JOIN CLIENTE C ON P.cliente_id = C.id
                          WHERE C.nombre LIKE @f OR P.estado LIKE @f OR C.identificacion LIKE @f
                             OR CAST(P.id AS NVARCHAR) LIKE @f
                          ORDER BY P.id DESC";

        var dtEnc = ExecuteQuery(sqlEnc, new[] { new SqlParameter("@f", $"%{filtro}%") }, false);
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
        string sql = @"SELECT P.*, C.nombre as ClienteNombre, C.identificacion as ClienteIdentificacion 
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
        ExecuteNonQuery(sql, new[] { new SqlParameter("@id", id) }, false);
    }

    private List<ProformaDetalleDTO> ObtenerDetallesProforma(int proformaId)
    {
        var detalles = new List<ProformaDetalleDTO>();
        string sqlDet = @"SELECT D.*, P.nombre as ProductoNombre, P.codigo_barras
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
                ProductoCodigo = rd["codigo_barras"]?.ToString() ?? rd["producto_id"].ToString(),
                Cantidad = Convert.ToDecimal(rd["cantidad"]),
                PrecioUnitario = Convert.ToDecimal(rd["precio_unitario"]),
                PorcentajeImpuesto = rd.Table.Columns.Contains("porcentaje_impuesto") ? Convert.ToDecimal(rd["porcentaje_impuesto"]) : 0,
                ImpuestoTotal = rd.Table.Columns.Contains("monto_impuesto") ? Convert.ToDecimal(rd["monto_impuesto"]) : 0,
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
            ClienteIdentificacion = r.Table.Columns.Contains("ClienteIdentificacion") ? r["ClienteIdentificacion"].ToString() : string.Empty,
            SucursalId = r.Table.Columns.Contains("sucursal_id") ? Convert.ToInt32(r["sucursal_id"]) : 0,
            TotalNeto = r.Table.Columns.Contains("total_neto") ? Convert.ToDecimal(r["total_neto"]) : 0,
            TotalImpuesto = r.Table.Columns.Contains("total_impuesto") ? Convert.ToDecimal(r["total_impuesto"]) : 0,
            Total = Convert.ToDecimal(r["total"]),
            Fecha = Convert.ToDateTime(r["fecha"]),
            FechaVencimiento = r.Table.Columns.Contains("fecha_vencimiento") ? Convert.ToDateTime(r["fecha_vencimiento"]) : DateTime.MinValue,
            Estado = r["estado"].ToString() ?? "Pendiente"
        };
    }

    public List<ProformaEncabezadoDTO> ListarPorCliente(int clienteId)
    {
        string sqlEnc = @"SELECT P.*, C.nombre as ClienteNombre, C.identificacion as ClienteIdentificacion 
                      FROM PROFORMA_ENCABEZADO P
                      JOIN CLIENTE C ON P.cliente_id = C.id
                      WHERE P.cliente_id = @cid
                      ORDER BY P.id DESC";

        var dtEnc = ExecuteQuery(sqlEnc, new[] { new SqlParameter("@cid", clienteId) }, false);
        var lista = new List<ProformaEncabezadoDTO>();

        foreach (DataRow r in dtEnc.Rows)
        {
            var proforma = MapearEncabezado(r);
            proforma.Detalles = ObtenerDetallesProforma(proforma.Id);
            lista.Add(proforma);
        }
        return lista;
    }
    public string ConvertirAFactura(int proformaId, int usuarioId, string medioPago)
    {
        string sql = @"
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @clienteId INT, @sucursalId INT, @totalNeto DECIMAL(18,2), @totalImpuesto DECIMAL(18,2), 
            @totalComprobante DECIMAL(18,2), @facturaId INT, @estadoActual NVARCHAR(20);
    DECLARE @consecutivo NVARCHAR(50) = 'FAC-' + CAST(NEXT VALUE FOR Seq_Consecutivos AS NVARCHAR(20));

    -- 1. Obtener datos de proforma
    SELECT @clienteId = cliente_id, @sucursalId = sucursal_id, @totalNeto = total_neto, 
           @totalImpuesto = total_impuesto, @totalComprobante = total, @estadoActual = estado 
    FROM PROFORMA_ENCABEZADO WHERE id = @profId;

    -- 2. Validaciones de estado
    IF @clienteId IS NULL THROW 50000, 'La proforma no existe.', 1;
    IF @estadoActual = 'Facturada' THROW 50002, 'Esta proforma ya fue facturada.', 1;
    IF @estadoActual = 'Anulada' THROW 50003, 'No se puede facturar una proforma anulada.', 1;

    -- 3. VALIDAR STOCK (Preventivo)
    IF EXISTS (
        SELECT 1 FROM PRODUCTO P INNER JOIN PROFORMA_DETALLE D ON P.id = D.producto_id 
        WHERE D.proforma_id = @profId AND P.existencia < D.cantidad
    )
    BEGIN
        THROW 50001, 'Stock insuficiente para procesar la conversión.', 1;
    END

    -- 4. Crear Factura Encabezado
    INSERT INTO FACTURA_ENCABEZADO (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, 
                                  total_neto, total_impuesto, total_comprobante, medio_pago, 
                                  estado_hacienda, condicion_venta, fecha, es_offline)
    VALUES (@clienteId, @sucursalId, @usuarioId, @consecutivo, '00000', 
            @totalNeto, @totalImpuesto, @totalComprobante, @medioPago, 'LOCAL', 'Contado', GETDATE(), 1);
    
    SET @facturaId = SCOPE_IDENTITY();

    -- 5. Crear Factura Detalle
    INSERT INTO FACTURA_DETALLE (factura_id, producto_id, cantidad, precio_unitario, porcentaje_impuesto, monto_impuesto, total_linea)
    SELECT @facturaId, d.producto_id, d.cantidad, d.precio_unitario, d.porcentaje_impuesto, d.monto_impuesto, d.total_linea
    FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

    -- 6. REGISTRAR EN KARDEX (Esto dispara el Trigger y rebaja el stock UNA SOLA VEZ)
    INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas)
    SELECT d.producto_id, @usuarioId, GETDATE(), 'SALIDA', d.cantidad, @consecutivo, 'Fact. desde Proforma #' + CAST(@profId AS NVARCHAR(10))
    FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

    -- 7. Finalizar Proforma
    UPDATE PROFORMA_ENCABEZADO SET estado = 'Facturada' WHERE id = @profId;

    COMMIT TRANSACTION;
    SELECT @consecutivo;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH";

        var parametros = new[] {
        new SqlParameter("@profId", proformaId),
        new SqlParameter("@usuarioId", usuarioId),
        new SqlParameter("@medioPago", medioPago)
    };

        object result = ExecuteScalar(sql, parametros, false);
        return result?.ToString() ?? string.Empty;
    }
}