using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;
using IntegraPro.DataAccess.Dao;
using System.Collections.Generic;
using System;

namespace IntegraPro.DataAccess.Factory;

public class CompraFactory(string connectionString) : MasterDao(connectionString)
{
    private readonly string _connectionString = connectionString;

    public void ProcesarCompra(CompraDTO compra, UsuarioDTO ejecutor)
    {
        // === INTEGRACIÓN DE SEGURIDAD ROBUSTA (MODIFICADO PARA DIAGNÓSTICO) ===

        // Si el diccionario de permisos está vacío, lanzamos un error detallado para saber qué está llegando
        if (ejecutor.Permisos == null || ejecutor.Permisos.Count == 0)
        {
            throw new UnauthorizedAccessException($"Acceso denegado: El sistema no detectó permisos cargados. (JSON: {ejecutor.PermisosJson})");
        }

        // 1. Verificamos si es Admin o tiene el permiso específico
        bool tieneAcceso = ejecutor.TienePermiso("all") || ejecutor.TienePermiso("compras");

        // 2. Verificamos la restricción de solo_lectura directamente en el diccionario
        bool esSoloLectura = ejecutor.Permisos.TryGetValue("solo_lectura", out bool sl) && sl;

        if (!tieneAcceso)
            throw new UnauthorizedAccessException("No tiene permisos para procesar compras.");

        if (esSoloLectura)
            throw new UnauthorizedAccessException("Su usuario tiene restricciones de solo lectura y no puede realizar transacciones.");

        // ==========================================

        if (ejecutor.TienePermiso("sucursal_limit"))
            compra.SucursalId = ejecutor.SucursalId;

        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            string sqlEnc = @"INSERT INTO COMPRA_ENCABEZADO 
                (proveedor_id, sucursal_id, usuario_id, numero_factura_proveedor, fecha_compra, subtotal, total_impuestos, total_compra, notas, estado, tipo_pago) 
                VALUES (@prov, @suc, @usu, @num, @fecC, @sub, @imp, @tot, @not, 'Procesada', @tPag);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            using var cmdEnc = new SqlCommand(sqlEnc, conn, trans);
            cmdEnc.Parameters.AddWithValue("@prov", compra.ProveedorId);
            cmdEnc.Parameters.AddWithValue("@suc", compra.SucursalId);
            cmdEnc.Parameters.AddWithValue("@usu", ejecutor.Id);
            cmdEnc.Parameters.AddWithValue("@num", (object?)compra.NumeroFacturaProveedor ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@fecC", compra.FechaCompra);
            cmdEnc.Parameters.AddWithValue("@sub", compra.Subtotal);
            cmdEnc.Parameters.AddWithValue("@imp", compra.TotalImpuestos);
            cmdEnc.Parameters.AddWithValue("@tot", compra.TotalCompra);
            cmdEnc.Parameters.AddWithValue("@not", (object?)compra.Notas ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@tPag", compra.TipoPago);

            int compraId = (int)cmdEnc.ExecuteScalar();

            if (compra.TipoPago == "Credito")
            {
                string sqlCxp = @"INSERT INTO CUENTA_PAGAR 
                    (compra_id, proveedor_id, monto_total, saldo_pendiente, fecha_vencimiento, estado) 
                    VALUES (@cId, @pId, @tot, @tot, @fVen, 'Pendiente')";

                using var cmdCxp = new SqlCommand(sqlCxp, conn, trans);
                cmdCxp.Parameters.AddWithValue("@cId", compraId);
                cmdCxp.Parameters.AddWithValue("@pId", compra.ProveedorId);
                cmdCxp.Parameters.AddWithValue("@tot", compra.TotalCompra);
                cmdCxp.Parameters.AddWithValue("@fVen", compra.FechaCompra.AddDays(compra.DiasCredito));
                cmdCxp.ExecuteNonQuery();
            }

            foreach (var det in compra.Detalles)
            {
                string sqlDet = "INSERT INTO COMPRA_DETALLE (compra_id, producto_id, cantidad, costo_unitario_neto, monto_impuesto, total_linea) VALUES (@cId, @pId, @can, @cos, @mImp, @totL)";
                using var cmdDet = new SqlCommand(sqlDet, conn, trans);
                cmdDet.Parameters.AddWithValue("@cId", compraId);
                cmdDet.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdDet.Parameters.AddWithValue("@can", det.Cantidad);
                cmdDet.Parameters.AddWithValue("@cos", det.CostoUnitarioNeto);
                cmdDet.Parameters.AddWithValue("@mImp", det.MontoImpuesto);
                cmdDet.Parameters.AddWithValue("@totL", det.TotalLinea);
                cmdDet.ExecuteNonQuery();

                string sqlMov = @"INSERT INTO MOVIMIENTO_INVENTARIO 
                    (producto_id, usuario_id, sucursal_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) 
                    VALUES (@pId, @uId, @sId, GETDATE(), 'Entrada', @can, @ref, 'Compra Recibida')";

                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdMov.Parameters.AddWithValue("@uId", ejecutor.Id);
                cmdMov.Parameters.AddWithValue("@sId", compra.SucursalId);
                cmdMov.Parameters.AddWithValue("@can", det.Cantidad);
                cmdMov.Parameters.AddWithValue("@ref", "FAC-" + (compra.NumeroFacturaProveedor ?? "S/N"));
                cmdMov.ExecuteNonQuery();

                string sqlCosto = "UPDATE PRODUCTO SET costo_actual = @nCos WHERE id = @pId";
                using var cmdCosto = new SqlCommand(sqlCosto, conn, trans);
                cmdCosto.Parameters.AddWithValue("@nCos", det.CostoUnitarioNeto);
                cmdCosto.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdCosto.ExecuteNonQuery();

                if (!string.IsNullOrEmpty(det.CodigoCabys) && det.ProductoId > 0)
                {
                    string sqlEquiv = @"
                        IF EXISTS (SELECT 1 FROM PRODUCTO_EQUIVALENCIA WHERE proveedor_id = @provId AND codigo_xml = @codXml)
                            UPDATE PRODUCTO_EQUIVALENCIA SET producto_id = @prodId WHERE proveedor_id = @provId AND codigo_xml = @codXml
                        ELSE
                            INSERT INTO PRODUCTO_EQUIVALENCIA (proveedor_id, codigo_xml, producto_id) VALUES (@provId, @codXml, @prodId)";

                    using var cmdEquiv = new SqlCommand(sqlEquiv, conn, trans);
                    cmdEquiv.Parameters.AddWithValue("@provId", compra.ProveedorId);
                    cmdEquiv.Parameters.AddWithValue("@codXml", det.CodigoCabys);
                    cmdEquiv.Parameters.AddWithValue("@prodId", det.ProductoId);
                    cmdEquiv.ExecuteNonQuery();
                }
            }
            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    public List<DeudaConsultaDTO> BuscarDeudas(string filtro, UsuarioDTO ejecutor)
    {
        var lista = new List<DeudaConsultaDTO>();
        using var conn = new SqlConnection(_connectionString);

        string sucursalFilter = ejecutor.TienePermiso("sucursal_limit")
            ? $" AND CompraId IN (SELECT id FROM COMPRA_ENCABEZADO WHERE sucursal_id = {ejecutor.SucursalId})"
            : "";

        string sql = $@"SELECT * FROM VW_CONSULTA_DEUDAS 
                       WHERE (ProveedorCedula LIKE @f 
                          OR ProveedorNombre LIKE @f 
                          OR NumeroFactura LIKE @f)
                          {sucursalFilter}
                       ORDER BY FechaVencimiento ASC";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@f", $"%{filtro}%");

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            lista.Add(new DeudaConsultaDTO
            {
                DeudaId = (int)reader["DeudaId"],
                CompraId = (int)reader["CompraId"],
                ProveedorCedula = reader["ProveedorCedula"].ToString()!,
                ProveedorNombre = reader["ProveedorNombre"].ToString()!,
                NumeroFactura = reader["NumeroFactura"].ToString()!,
                FechaCompra = (DateTime)reader["FechaCompra"],
                MontoOriginal = (decimal)reader["MontoOriginal"],
                SaldoActual = (decimal)reader["SaldoActual"],
                FechaVencimiento = (DateTime)reader["FechaVencimiento"],
                EstadoDeuda = reader["EstadoDeuda"].ToString()!
            });
        }
        return lista;
    }

    public List<PagoHistorialDTO> ObtenerHistorialPagos(int compraId)
    {
        var historial = new List<PagoHistorialDTO>();
        using var conn = new SqlConnection(_connectionString);

        string sql = @"SELECT H.id, H.monto, H.fecha_pago, H.numero_referencia, H.notas, U.nombre_completo as nombre_usuario
                       FROM PAGO_CXP_HISTORIAL H
                       JOIN USUARIO U ON H.usuario_id = U.id
                       WHERE H.compra_id = @cId
                       ORDER BY H.fecha_pago DESC";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@cId", compraId);

        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            historial.Add(new PagoHistorialDTO
            {
                Id = (int)reader["id"],
                Monto = (decimal)reader["monto"],
                FechaPago = (DateTime)reader["fecha_pago"],
                NumeroReferencia = reader["numero_referencia"]?.ToString(),
                Notas = reader["notas"]?.ToString(),
                NombreUsuario = reader["nombre_usuario"]?.ToString()
            });
        }
        return historial;
    }

    public void RegistrarPagoCxp(PagoCxpDTO pago, UsuarioDTO ejecutor)
    {
        bool esSoloLectura = ejecutor.Permisos.TryGetValue("solo_lectura", out bool sl) && sl;
        if (esSoloLectura)
            throw new UnauthorizedAccessException("El rol de auditoría no puede registrar pagos.");

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        string sqlCheck = "SELECT saldo_pendiente FROM CUENTA_PAGAR WHERE compra_id = @cId AND estado != 'Anulado'";
        using (var cmdCheck = new SqlCommand(sqlCheck, conn))
        {
            cmdCheck.Parameters.AddWithValue("@cId", pago.CompraId);
            var result = cmdCheck.ExecuteScalar();
            if (result == null) throw new Exception("Error: Esta compra no genera deuda.");
            decimal saldoActual = Convert.ToDecimal(result);
            if (saldoActual <= 0) throw new Exception("Esta factura ya ha sido pagada.");
            if (pago.Monto > saldoActual) throw new Exception($"Monto excede el saldo ({saldoActual}).");
        }

        using var trans = conn.BeginTransaction();
        try
        {
            string sqlHistorial = "INSERT INTO PAGO_CXP_HISTORIAL (compra_id, usuario_id, monto, fecha_pago, numero_referencia, notas) VALUES (@cId, @uId, @monto, GETDATE(), @ref, @not)";
            using (var cmdHist = new SqlCommand(sqlHistorial, conn, trans))
            {
                cmdHist.Parameters.AddWithValue("@cId", pago.CompraId);
                cmdHist.Parameters.AddWithValue("@uId", ejecutor.Id);
                cmdHist.Parameters.AddWithValue("@monto", pago.Monto);
                cmdHist.Parameters.AddWithValue("@ref", (object?)pago.NumeroReferencia ?? DBNull.Value);
                cmdHist.Parameters.AddWithValue("@not", (object?)pago.Notas ?? DBNull.Value);
                cmdHist.ExecuteNonQuery();
            }

            string sqlUpdateCxp = @"UPDATE CUENTA_PAGAR SET saldo_pendiente = saldo_pendiente - @monto,
                                    estado = CASE WHEN (saldo_pendiente - @monto) <= 0 THEN 'Pagada' ELSE 'Pendiente' END
                                    WHERE compra_id = @cId";
            using (var cmdUp = new SqlCommand(sqlUpdateCxp, conn, trans))
            {
                cmdUp.Parameters.AddWithValue("@monto", pago.Monto);
                cmdUp.Parameters.AddWithValue("@cId", pago.CompraId);
                cmdUp.ExecuteNonQuery();
            }
            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    public void AnularCompra(int compraId, UsuarioDTO ejecutor)
    {
        bool tieneAcceso = ejecutor.TienePermiso("all") || ejecutor.TienePermiso("compras");
        bool esSoloLectura = ejecutor.Permisos.TryGetValue("solo_lectura", out bool sl) && sl;

        if (!tieneAcceso || esSoloLectura)
            throw new UnauthorizedAccessException("No tiene permisos para anular compras.");

        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            var detalles = new List<(int pId, decimal cant, string refProv, int sId)>();
            string sqlGet = @"SELECT D.producto_id, D.cantidad, E.numero_factura_proveedor, E.sucursal_id 
                          FROM COMPRA_DETALLE D 
                          JOIN COMPRA_ENCABEZADO E ON D.compra_id = E.id 
                          WHERE E.id = @id AND E.estado = 'Procesada'";

            using (var cmdGet = new SqlCommand(sqlGet, conn, trans))
            {
                cmdGet.Parameters.AddWithValue("@id", compraId);
                using var reader = cmdGet.ExecuteReader();
                while (reader.Read())
                    detalles.Add(((int)reader[0], (decimal)reader[1], reader[2]?.ToString() ?? "S/N", (int)reader[3]));
            }

            if (detalles.Count == 0) throw new Exception("Compra no encontrada o no es anulable.");

            if (ejecutor.TienePermiso("sucursal_limit") && detalles[0].sId != ejecutor.SucursalId)
                throw new UnauthorizedAccessException("No puede anular compras de otra sucursal.");

            foreach (var det in detalles)
            {
                string sqlMov = @"INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, sucursal_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) 
                              VALUES (@pId, @uId, @sId, GETDATE(), 'Salida', @can, @ref, 'ANULACIÓN DE COMPRA')";
                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.pId);
                cmdMov.Parameters.AddWithValue("@uId", ejecutor.Id);
                cmdMov.Parameters.AddWithValue("@sId", det.sId);
                cmdMov.Parameters.AddWithValue("@can", det.cant);
                cmdMov.Parameters.AddWithValue("@ref", "ANUL-" + det.refProv);
                cmdMov.ExecuteNonQuery();
            }

            string sqlUpdateCxp = "UPDATE CUENTA_PAGAR SET estado = 'Anulado', saldo_pendiente = 0 WHERE compra_id = @id";
            using (var cmdUp1 = new SqlCommand(sqlUpdateCxp, conn, trans))
            {
                cmdUp1.Parameters.AddWithValue("@id", compraId);
                cmdUp1.ExecuteNonQuery();
            }

            string sqlUpdateEnc = "UPDATE COMPRA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id";
            using (var cmdUp2 = new SqlCommand(sqlUpdateEnc, conn, trans))
            {
                cmdUp2.Parameters.AddWithValue("@id", compraId);
                cmdUp2.ExecuteNonQuery();
            }

            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    public List<AlertaPagoDTO> ObtenerAlertasPagos(UsuarioDTO ejecutor)
    {
        var alertas = new List<AlertaPagoDTO>();
        string sql = "SELECT * FROM VW_ALERTA_CUENTAS_PAGAR_PROXIMAS";
        if (ejecutor.TienePermiso("sucursal_limit"))
            sql += " WHERE sucursal_id = " + ejecutor.SucursalId;
        sql += " ORDER BY fecha_vencimiento ASC";

        var dt = ExecuteQuery(sql, null, false);
        foreach (DataRow row in dt.Rows)
        {
            alertas.Add(new AlertaPagoDTO
            {
                Id = (int)row["id"],
                ProveedorNombre = row["proveedor_nombre"].ToString()!,
                SaldoPendiente = (decimal)row["saldo_pendiente"],
                FechaVencimiento = (DateTime)row["fecha_vencimiento"],
                DiasParaVencimiento = (int)row["dias_para_vencimiento"],
                Urgencia = row["urgencia"].ToString()!
            });
        }
        return alertas;
    }

    public ResumenCxpDTO ObtenerResumenGeneralCxp(UsuarioDTO ejecutor)
    {
        string filter = ejecutor.TienePermiso("sucursal_limit")
            ? $" AND C.compra_id IN (SELECT id FROM COMPRA_ENCABEZADO WHERE sucursal_id = {ejecutor.SucursalId})"
            : "";

        string sql = $@"SELECT 
                        SUM(saldo_pendiente) as TotalGlobal,
                        COUNT(id) as TotalFacturas,
                        COUNT(DISTINCT proveedor_id) as TotalProveedores,
                        MIN(fecha_vencimiento) as ProximaFecha
                       FROM CUENTA_PAGAR C
                       WHERE estado = 'Pendiente' AND saldo_pendiente > 0 {filter}";

        var dt = ExecuteQuery(sql, null, false);
        if (dt != null && dt.Rows.Count > 0)
        {
            var row = dt.Rows[0];
            return new ResumenCxpDTO
            {
                TotalDeudaGlobal = row["TotalGlobal"] != DBNull.Value ? (decimal)row["TotalGlobal"] : 0,
                FacturasPendientes = row["TotalFacturas"] != DBNull.Value ? (int)row["TotalFacturas"] : 0,
                ProveedoresAQuienesSeDebe = row["TotalProveedores"] != DBNull.Value ? (int)row["TotalProveedores"] : 0,
                FechaProximoVencimiento = row["ProximaFecha"] != DBNull.Value ? (DateTime)row["ProximaFecha"] : null
            };
        }
        return new ResumenCxpDTO();
    }
}