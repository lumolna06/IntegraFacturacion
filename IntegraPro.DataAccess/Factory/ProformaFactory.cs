using IntegraPro.DataAccess.Dao;
using IntegraPro.DTO.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class ProformaFactory(string connectionString) : MasterDao(connectionString)
{
    /// <summary>
    /// Crea una proforma validando permisos y forzando sucursal si el usuario está limitado.
    /// </summary>
    public int CrearProforma(ProformaEncabezadoDTO p, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("proformas"))
            throw new UnauthorizedAccessException("Su rol no tiene permisos para crear proformas.");

        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Su usuario es de solo lectura.");

        if (ejecutor.TienePermiso("sucursal_limit"))
            p.SucursalId = ejecutor.SucursalId;

        var configFact = new ConfiguracionFactory(connectionString);
        var empresa = configFact.ObtenerEmpresa();
        bool esTradicional = empresa == null || empresa.TipoRegimen.Equals("Tradicional", StringComparison.OrdinalIgnoreCase);

        decimal totalNetoAcumulado = 0;
        decimal totalIVAAcumulado = 0;

        foreach (var det in p.Detalles)
        {
            string sqlValidar = "SELECT costo_actual, porcentaje_impuesto FROM PRODUCTO WHERE id = @prodid";
            var dt = ExecuteQuery(sqlValidar, new[] { new SqlParameter("@prodid", det.ProductoId) }, false);

            if (dt == null || dt.Rows.Count == 0)
                throw new Exception($"El producto con ID {det.ProductoId} no existe.");

            DataRow row = dt.Rows[0];
            decimal precioVigente = Convert.ToDecimal(row["costo_actual"]);
            decimal pctIVA = Convert.ToDecimal(row["porcentaje_impuesto"]);

            decimal subtotalLinea = precioVigente * (decimal)det.Cantidad;
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
            new SqlParameter("@cid", p.ClienteId == 0 ? (object)DBNull.Value : p.ClienteId),
            new SqlParameter("@sid", p.SucursalId),
            new SqlParameter("@fvenc", p.FechaVencimiento),
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

    /// <summary>
    /// Actualiza los datos básicos de una proforma (Encabezado).
    /// </summary>
    public void ActualizarProforma(ProformaEncabezadoDTO p, UsuarioDTO ejecutor)
    {
        if (ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Perfil de solo lectura.");

        string sql = "UPDATE PROFORMA_ENCABEZADO SET fecha_vencimiento = @fvenc, cliente_id = @cid WHERE id = @id";

        var pars = new List<SqlParameter> {
            new SqlParameter("@fvenc", p.FechaVencimiento),
            new SqlParameter("@cid", p.ClienteId == 0 ? (object)DBNull.Value : p.ClienteId),
            new SqlParameter("@id", p.Id)
        };

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        int filas = ExecuteNonQuery(sql, pars.ToArray(), false);
        if (filas == 0) throw new Exception("No se pudo actualizar la proforma o no pertenece a su sucursal.");
    }

    public List<ProformaEncabezadoDTO> ListarProformas(string filtro, UsuarioDTO ejecutor)
    {
        return EjecutarConsultaBase(filtro, null, ejecutor);
    }

    public List<ProformaEncabezadoDTO> ListarPorCliente(int clienteId, UsuarioDTO ejecutor)
    {
        return EjecutarConsultaBase(string.Empty, clienteId, ejecutor);
    }

    private List<ProformaEncabezadoDTO> EjecutarConsultaBase(string filtro, int? clienteId, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("proformas")) return new List<ProformaEncabezadoDTO>();

        string sql = @"SELECT P.*, C.nombre as ClienteNombre, C.identificacion as ClienteIdentificacion 
                       FROM PROFORMA_ENCABEZADO P
                       LEFT JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE 1=1";

        var pars = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(filtro))
        {
            sql += " AND (C.nombre LIKE @f OR P.estado LIKE @f OR CAST(P.id AS NVARCHAR) LIKE @f)";
            pars.Add(new SqlParameter("@f", $"%{filtro}%"));
        }

        if (clienteId.HasValue)
        {
            sql += " AND P.cliente_id = @cid";
            pars.Add(new SqlParameter("@cid", clienteId.Value));
        }

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND P.sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        sql += " ORDER BY P.id DESC";

        var dt = ExecuteQuery(sql, pars.ToArray(), false);
        var lista = new List<ProformaEncabezadoDTO>();
        foreach (DataRow r in dt.Rows)
        {
            lista.Add(MapearEncabezado(r));
        }
        return lista;
    }

    public ProformaEncabezadoDTO? ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        string sql = @"SELECT P.*, C.nombre as ClienteNombre, C.identificacion as ClienteIdentificacion 
                       FROM PROFORMA_ENCABEZADO P
                       LEFT JOIN CLIENTE C ON P.cliente_id = C.id
                       WHERE P.id = @id";

        var pars = new List<SqlParameter> { new SqlParameter("@id", id) };

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND P.sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        var dt = ExecuteQuery(sql, pars.ToArray(), false);
        if (dt == null || dt.Rows.Count == 0) return null;

        var proforma = MapearEncabezado(dt.Rows[0]);
        proforma.Detalles = ObtenerDetallesProforma(id);
        return proforma;
    }

    public void AnularProforma(int id, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("proformas") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("Permisos insuficientes.");

        string sql = "UPDATE PROFORMA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id AND estado = 'Pendiente'";
        var pars = new List<SqlParameter> { new SqlParameter("@id", id) };

        if (ejecutor.TienePermiso("sucursal_limit"))
        {
            sql += " AND sucursal_id = @sid";
            pars.Add(new SqlParameter("@sid", ejecutor.SucursalId));
        }

        int filas = ExecuteNonQuery(sql, pars.ToArray(), false);
        if (filas == 0) throw new Exception("No se pudo anular (ya facturada o sin acceso).");
    }

    public string ConvertirAFactura(int proformaId, int usuarioIdAudit, string medioPago, UsuarioDTO ejecutor)
    {
        if (!ejecutor.TienePermiso("ventas") || ejecutor.TienePermiso("solo_lectura"))
            throw new UnauthorizedAccessException("No tiene permisos para ventas.");

        string sql = @"
        BEGIN TRANSACTION;
        BEGIN TRY
            DECLARE @clienteId INT, @sucursalId INT, @totalNeto DECIMAL(18,2), @totalImpuesto DECIMAL(18,2), 
                    @totalComprobante DECIMAL(18,2), @facturaId INT, @estadoActual NVARCHAR(20),
                    @permitirNegativo BIT;

            DECLARE @consecutivo NVARCHAR(50) = 'FAC-' + CAST(NEXT VALUE FOR Seq_Consecutivos AS NVARCHAR(20));

            -- 1. Obtener datos de la proforma
            SELECT @clienteId = cliente_id, @sucursalId = sucursal_id, @totalNeto = total_neto, 
                   @totalImpuesto = total_impuesto, @totalComprobante = total, @estadoActual = estado 
            FROM PROFORMA_ENCABEZADO WHERE id = @profId;

            -- 2. Obtener configuración de empresa
            SELECT TOP 1 @permitirNegativo = permitir_stock_negativo FROM EMPRESA;

            IF @clienteId IS NULL THROW 50000, 'La proforma no existe.', 1;
            IF @estadoActual <> 'Pendiente' THROW 50002, 'Solo se pueden facturar proformas Pendientes.', 1;
            IF @sucursalLimit = 1 AND @sucursalId <> @userSucId THROW 50003, 'Acceso denegado a esta sucursal.', 1;

            -- 3. VALIDACIÓN DE STOCK (Si NO se permiten negativos)
            IF ISNULL(@permitirNegativo, 0) = 0
            BEGIN
                IF EXISTS (
                    SELECT 1 
                    FROM PROFORMA_DETALLE d
                    JOIN PRODUCTO p ON d.producto_id = p.id
                    WHERE d.proforma_id = @profId AND p.existencia < d.cantidad
                )
                BEGIN
                    THROW 50005, 'Stock insuficiente para completar la facturación de uno o más productos.', 1;
                END
            END

            -- 4. Crear factura
            INSERT INTO FACTURA_ENCABEZADO (cliente_id, sucursal_id, usuario_id, consecutivo, clave_numerica, 
                                          total_neto, total_impuesto, total_comprobante, medio_pago, 
                                          estado_hacienda, condicion_venta, fecha, es_offline)
            VALUES (@clienteId, @sucursalId, @usuarioId, @consecutivo, '00000', 
                    @totalNeto, @totalImpuesto, @totalComprobante, @medioPago, 'LOCAL', 'Contado', GETDATE(), 1);
            
            SET @facturaId = SCOPE_IDENTITY();

            INSERT INTO FACTURA_DETALLE (factura_id, producto_id, cantidad, precio_unitario, porcentaje_impuesto, monto_impuesto, total_linea)
            SELECT @facturaId, d.producto_id, d.cantidad, d.precio_unitario, d.porcentaje_impuesto, d.monto_impuesto, d.total_linea
            FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

            -- 5. Registro de movimientos de inventario (resta stock)
            INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, sucursal_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas)
            SELECT d.producto_id, @usuarioId, @sucursalId, GETDATE(), 'SALIDA', d.cantidad, @consecutivo, 'Fact. desde Proforma #' + CAST(@profId AS NVARCHAR(10))
            FROM PROFORMA_DETALLE d WHERE d.proforma_id = @profId;

            -- 6. Cerrar proforma
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
            new SqlParameter("@medioPago", medioPago)
        };

        object result = ExecuteScalar(sql, parametros, false);
        return result?.ToString() ?? string.Empty;
    }

    public List<ProformaDetalleDTO> ObtenerDetallesProforma(int proformaId)
    {
        var detalles = new List<ProformaDetalleDTO>();
        string sqlDet = @"SELECT D.*, P.nombre as ProductoNombre, P.codigo_barras
                          FROM PROFORMA_DETALLE D
                          JOIN PRODUCTO P ON D.producto_id = P.id
                          WHERE D.proforma_id = @pid";

        var dtDet = ExecuteQuery(sqlDet, new[] { new SqlParameter("@pid", proformaId) }, false);
        if (dtDet == null) return detalles;

        foreach (DataRow rd in dtDet.Rows)
        {
            detalles.Add(new ProformaDetalleDTO
            {
                ProductoId = Convert.ToInt32(rd["producto_id"]),
                ProductoNombre = rd["ProductoNombre"].ToString(),
                ProductoCodigo = rd["codigo_barras"]?.ToString() ?? rd["producto_id"].ToString(),
                Cantidad = Convert.ToDecimal(rd["cantidad"]),
                PrecioUnitario = Convert.ToDecimal(rd["precio_unitario"]),
                PorcentajeImpuesto = Convert.ToDecimal(rd["porcentaje_impuesto"]),
                ImpuestoTotal = Convert.ToDecimal(rd["monto_impuesto"]),
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
            ClienteId = r["cliente_id"] == DBNull.Value ? 0 : Convert.ToInt32(r["cliente_id"]),
            ClienteNombre = r["ClienteNombre"]?.ToString() ?? "CLIENTE CONTADO",
            ClienteIdentificacion = r.Table.Columns.Contains("ClienteIdentificacion") ? r["ClienteIdentificacion"].ToString() : string.Empty,
            SucursalId = Convert.ToInt32(r["sucursal_id"]),
            TotalNeto = Convert.ToDecimal(r["total_neto"]),
            TotalImpuesto = Convert.ToDecimal(r["total_impuesto"]),
            Total = Convert.ToDecimal(r["total"]),
            Fecha = Convert.ToDateTime(r["fecha"]),
            FechaVencimiento = Convert.ToDateTime(r["fecha_vencimiento"]),
            Estado = r["estado"].ToString() ?? "Pendiente"
        };
    }
}