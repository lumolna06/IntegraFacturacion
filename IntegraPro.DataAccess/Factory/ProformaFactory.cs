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

    public int CrearProforma(ProformaEncabezadoDTO p, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("proformas");
        ejecutor.ValidarEscritura();

        // Si el usuario tiene restricción de sucursal, forzamos la sucursal del usuario
        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            p.SucursalId = ejecutor.SucursalId;
        }

        var configFact = new ConfiguracionFactory(_connectionString);
        var empresa = configFact.ObtenerEmpresa(ejecutor);
        bool esTradicional = empresa?.TipoRegimen?.Equals("Tradicional", StringComparison.OrdinalIgnoreCase) ?? true;

        decimal totalNetoAcumulado = 0;
        decimal totalIVAAcumulado = 0;

        // Cálculo de totales línea por línea
        foreach (var det in p.Detalles)
        {
            // MODIFICACIÓN: Se cambia 'costo_actual' por 'precio_1'
            string sqlValidar = "SELECT precio_1, porcentaje_impuesto FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            if (dt == null || dt.Rows.Count == 0)
                throw new Exception($"El producto con ID {det.ProductoId} no existe.");

            DataRow row = dt.Rows[0];
            // MODIFICACIÓN: Se asigna precio_1 como el precio de venta vigente
            decimal precioVigente = Convert.ToDecimal(row["precio_1"]);
            decimal pctIVA = Convert.ToDecimal(row["porcentaje_impuesto"]);

            decimal subtotalLinea = precioVigente * det.Cantidad;
            decimal impuestoLinea = esTradicional ? (subtotalLinea * (pctIVA / 100)) : 0;

            det.PrecioUnitario = precioVigente;
            det.PorcentajeImpuesto = esTradicional ? pctIVA : 0;
            det.ImpuestoTotal = impuestoLinea;
            det.TotalLineas = subtotalLinea + impuestoLinea;

            totalNetoAcumulado += subtotalLinea;
            totalIVAAcumulado += impuestoLinea;
        }

        string sqlEnc = @"INSERT INTO PROFORMA_ENCABEZADO 
                          (cliente_id, sucursal_id, fecha, fecha_vencimiento, total_neto, total_impuesto, total, estado)
                          VALUES (@cid, @sid, GETDATE(), @fvenc, @tneto, @tiva, @total, 'Pendiente');
                          SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var parametrosEnc = new[] {
            new SqlParameter("@cid", p.ClienteId == 0 ? DBNull.Value : p.ClienteId),
            new SqlParameter("@sid", p.SucursalId),
            new SqlParameter("@fvenc", p.FechaVencimiento == DateTime.MinValue ? DateTime.Now.AddDays(30) : p.FechaVencimiento),
            new SqlParameter("@tneto", totalNetoAcumulado),
            new SqlParameter("@tiva", totalIVAAcumulado),
            new SqlParameter("@total", totalNetoAcumulado + totalIVAAcumulado)
        };

        int idGenerado = (int)ExecuteScalar(sqlEnc, parametrosEnc, false);

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

    public string ConvertirAFactura(int proformaId, string medioPago, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("ventas");
        ejecutor.ValidarEscritura();

        string sql = @"
        BEGIN TRANSACTION;
        BEGIN TRY
            DECLARE @clienteId INT, @sucursalId INT, @totalNeto DECIMAL(18,2), @totalImpuesto DECIMAL(18,2), 
                    @totalComprobante DECIMAL(18,2), @facturaId INT, @estadoActual NVARCHAR(20),
                    @permitirNegativo BIT, @stockActual DECIMAL(18,2), @prodNombreErr NVARCHAR(200);

            DECLARE @consecutivo NVARCHAR(50) = 'FAC-' + CAST(NEXT VALUE FOR Seq_Consecutivos AS NVARCHAR(20));

            -- 1. Obtener datos de la proforma
            SELECT @clienteId = cliente_id, @sucursalId = sucursal_id, @totalNeto = total_neto, 
                   @totalImpuesto = total_impuesto, @totalComprobante = total, @estadoActual = estado 
            FROM PROFORMA_ENCABEZADO WHERE id = @profId;

            SELECT TOP 1 @permitirNegativo = permitir_stock_negativo FROM EMPRESA;

            IF @clienteId IS NULL THROW 50000, 'La proforma no existe.', 1;
            IF @estadoActual <> 'Pendiente' THROW 50002, 'Solo se pueden facturar proformas Pendientes.', 1;
            
            -- Validación de seguridad
            IF @esAdmin = 0 AND @sucursalLimit = 1 AND @sucursalId <> @userSucId 
                THROW 50003, 'Acceso denegado: Esta proforma pertenece a otra sucursal.', 1;

            -- 2. VALIDACIÓN DE STOCK EN SQL
            IF ISNULL(@permitirNegativo, 0) = 0
            BEGIN
                SELECT TOP 1 @prodNombreErr = p.nombre, @stockActual = ISNULL(ps.existencia, 0)
                FROM PROFORMA_DETALLE d
                INNER JOIN PRODUCTO p ON d.producto_id = p.id
                LEFT JOIN PRODUCTO_SUCURSAL ps ON d.producto_id = ps.producto_id AND ps.sucursal_id = @sucursalId
                WHERE d.proforma_id = @profId AND ISNULL(ps.existencia, 0) < d.cantidad;

                IF @prodNombreErr IS NOT NULL
                BEGIN
                    DECLARE @msg NVARCHAR(300) = 'Stock insuficiente de ' + @prodNombreErr + ' en Sucursal ' + CAST(@sucursalId AS NVARCHAR) + '. Disponible: ' + CAST(@stockActual AS NVARCHAR);
                    THROW 50005, @msg, 1;
                END
            END

            -- 3. Crear el encabezado de factura
            INSERT INTO FACTURA_ENCABEZADO (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, 
                                          total_neto, total_descuento, total_impuesto, total_comprobante, medio_pago, 
                                          estado_hacienda, condicion_venta, fecha)
            VALUES (@clienteId, @sucursalId, @usuarioId, @consecutivo, '00000', 
                    @totalNeto, 0, @totalImpuesto, @totalComprobante, @medioPago, 'Pendiente', 'Contado', GETDATE());
            
            SET @facturaId = SCOPE_IDENTITY();

            -- 4. Copiar detalles
            INSERT INTO FACTURA_DETALLE (factura_id, producto_id, cantidad, precio_unitario, porcentaje_descuento, monto_descuento, monto_impuesto, total_linea)
            SELECT @facturaId, d.producto_id, d.cantidad, d.precio_unitario, 0, 0, d.monto_impuesto, d.total_linea
            FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

            -- 5. Generar rebajos de inventario
            INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, sucursal_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas)
            SELECT d.producto_id, @usuarioId, @sucursalId, GETDATE(), 'SALIDA', d.cantidad, @consecutivo, 'Fact. desde Proforma #' + CAST(@profId AS NVARCHAR(10))
            FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

            -- 6. Actualizar estado
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
            new SqlParameter("@usuarioId", ejecutor.Id),
            new SqlParameter("@userSucId", ejecutor.SucursalId),
            new SqlParameter("@sucursalLimit", ejecutor.TienePermiso("sucursal_limit") ? 1 : 0),
            new SqlParameter("@esAdmin", ejecutor.TienePermiso("all") ? 1 : 0),
            new SqlParameter("@medioPago", medioPago)
        };

        object result = ExecuteScalar(sql, parametros, false);
        return result?.ToString() ?? string.Empty;
    }

    public void AnularProforma(int id, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("proformas");
        ejecutor.ValidarEscritura();

        string sql = "UPDATE PROFORMA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id AND estado = 'Pendiente'";
        var pars = new List<SqlParameter> { new SqlParameter("@id", id) };

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        int filas = ExecuteNonQuery(sql, pars.ToArray(), false);
        if (filas == 0)
            throw new Exception("No se pudo anular la proforma (ya está facturada o no pertenece a su sucursal).");
    }

    public List<ProformaEncabezadoDTO> ListarProformas(string filtro, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("proformas");

        string sql = @"SELECT P.*, ISNULL(C.nombre, 'CLIENTE CONTADO') as ClienteNombre, 
                       C.identificacion as ClienteIdentificacion 
                       FROM PROFORMA_ENCABEZADO P
                       LEFT JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE 1=1";

        var pars = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(filtro))
        {
            sql += " AND (C.nombre LIKE @f OR P.estado LIKE @f OR CAST(P.id AS NVARCHAR) LIKE @f)";
            pars.Add(new SqlParameter("@f", $"%{filtro}%"));
        }

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND P.sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        sql += " ORDER BY P.id DESC";

        var dt = ExecuteQuery(sql, pars.ToArray(), false);
        var lista = new List<ProformaEncabezadoDTO>();
        foreach (DataRow r in dt.Rows) lista.Add(MapearEncabezado(r));
        return lista;
    }

    public ProformaEncabezadoDTO? ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        ejecutor.ValidarAcceso("proformas");

        string sql = @"SELECT P.*, ISNULL(C.nombre, 'CLIENTE CONTADO') as ClienteNombre,
                       C.identificacion as ClienteIdentificacion 
                       FROM PROFORMA_ENCABEZADO P
                       LEFT JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE P.id = @id";

        if (!ejecutor.TienePermiso("all") && ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND P.sucursal_id = " + ejecutor.SucursalId;
        }

        var dt = ExecuteQuery(sql, new[] { new SqlParameter("@id", id) }, false);
        if (dt.Rows.Count == 0) return null;

        var proforma = MapearEncabezado(dt.Rows[0]);
        proforma.Detalles = ObtenerDetallesProforma(id);
        return proforma;
    }

    private List<ProformaDetalleDTO> ObtenerDetallesProforma(int proformaId)
    {
        var lista = new List<ProformaDetalleDTO>();
        string sql = @"SELECT D.*, P.nombre as ProductoNombre, P.codigo_barras as ProductoCodigo 
                       FROM PROFORMA_DETALLE D 
                       JOIN PRODUCTO P ON D.producto_id = P.id 
                       WHERE D.proforma_id = @id";

        var dt = ExecuteQuery(sql, new[] { new SqlParameter("@id", proformaId) }, false);
        foreach (DataRow r in dt.Rows)
        {
            lista.Add(new ProformaDetalleDTO
            {
                ProductoId = (int)r["producto_id"],
                ProductoCodigo = r["ProductoCodigo"].ToString(),
                ProductoNombre = r["ProductoNombre"].ToString(),
                Cantidad = (decimal)r["cantidad"],
                PrecioUnitario = (decimal)r["precio_unitario"],
                PorcentajeImpuesto = (decimal)r["porcentaje_impuesto"],
                ImpuestoTotal = (decimal)r["monto_impuesto"],
                TotalLineas = (decimal)r["total_linea"]
            });
        }
        return lista;
    }

    private ProformaEncabezadoDTO MapearEncabezado(DataRow r)
    {
        return new ProformaEncabezadoDTO
        {
            Id = Convert.ToInt32(r["id"]),
            ClienteId = r["cliente_id"] == DBNull.Value ? 0 : Convert.ToInt32(r["cliente_id"]),
            ClienteNombre = r["ClienteNombre"].ToString(),
            ClienteIdentificacion = r.Table.Columns.Contains("ClienteIdentificacion") ? r["ClienteIdentificacion"].ToString() : "",
            SucursalId = Convert.ToInt32(r["sucursal_id"]),
            TotalNeto = Convert.ToDecimal(r["total_neto"]),
            TotalImpuesto = Convert.ToDecimal(r["total_impuesto"]),
            Total = Convert.ToDecimal(r["total"]),
            Fecha = Convert.ToDateTime(r["fecha"]),
            FechaVencimiento = Convert.ToDateTime(r["fecha_vencimiento"]),
            Estado = r["estado"].ToString()
        };
    }
}