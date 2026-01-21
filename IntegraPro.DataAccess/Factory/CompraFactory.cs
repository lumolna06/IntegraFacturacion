using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;
using System.Data;

namespace IntegraPro.DataAccess.Factory;

public class CompraFactory(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public void ProcesarCompra(CompraDTO compra)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            // 1. Insertar Encabezado
            string sqlEnc = @"INSERT INTO COMPRA_ENCABEZADO 
                (proveedor_id, sucursal_id, usuario_id, numero_factura_proveedor, fecha_compra, subtotal, total_impuestos, total_compra, notas, estado, tipo_pago) 
                VALUES (@prov, @suc, @usu, @num, @fecC, @sub, @imp, @tot, @not, 'Procesada', @tPag);
                SELECT SCOPE_IDENTITY();";

            using var cmdEnc = new SqlCommand(sqlEnc, conn, trans);
            cmdEnc.Parameters.AddWithValue("@prov", compra.ProveedorId);
            cmdEnc.Parameters.AddWithValue("@suc", compra.SucursalId);
            cmdEnc.Parameters.AddWithValue("@usu", compra.UsuarioId);
            cmdEnc.Parameters.AddWithValue("@num", (object?)compra.NumeroFacturaProveedor ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@fecC", compra.FechaCompra);
            cmdEnc.Parameters.AddWithValue("@sub", compra.Subtotal);
            cmdEnc.Parameters.AddWithValue("@imp", compra.TotalImpuestos);
            cmdEnc.Parameters.AddWithValue("@tot", compra.TotalCompra);
            cmdEnc.Parameters.AddWithValue("@not", (object?)compra.Notas ?? DBNull.Value);
            cmdEnc.Parameters.AddWithValue("@tPag", compra.TipoPago);

            int compraId = Convert.ToInt32(cmdEnc.ExecuteScalar());

            // 2. Lógica de Cuenta por Pagar (CXP)
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

            // 3. Detalles, Inventario y Equivalencias
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

                string sqlMov = "INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) VALUES (@pId, @uId, GETDATE(), 'Entrada', @can, @ref, 'Compra Recibida')";
                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.ProductoId);
                cmdMov.Parameters.AddWithValue("@uId", compra.UsuarioId);
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

    public void RegistrarPagoCxp(PagoCxpDTO pago)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        string sqlCheck = "SELECT saldo_pendiente FROM CUENTA_PAGAR WHERE compra_id = @cId AND estado != 'Anulado'";
        using (var cmdCheck = new SqlCommand(sqlCheck, conn))
        {
            cmdCheck.Parameters.AddWithValue("@cId", pago.CompraId);
            var result = cmdCheck.ExecuteScalar();

            if (result == null)
                throw new Exception("Error: Esta compra no genera deuda (Posible compra de contado o inexistente).");

            decimal saldoActual = Convert.ToDecimal(result);
            if (saldoActual <= 0)
                throw new Exception("Aviso: Esta factura ya ha sido pagada en su totalidad.");

            if (pago.Monto > saldoActual)
                throw new Exception($"Error: El monto ingresado ({pago.Monto}) supera el saldo pendiente ({saldoActual}).");
        }

        using var trans = conn.BeginTransaction();
        try
        {
            string sqlHistorial = @"INSERT INTO PAGO_CXP_HISTORIAL 
                (compra_id, usuario_id, monto, fecha_pago, numero_referencia, notas) 
                VALUES (@cId, @uId, @monto, GETDATE(), @ref, @not)";

            using (var cmdHist = new SqlCommand(sqlHistorial, conn, trans))
            {
                cmdHist.Parameters.AddWithValue("@cId", pago.CompraId);
                cmdHist.Parameters.AddWithValue("@uId", pago.UsuarioId);
                cmdHist.Parameters.AddWithValue("@monto", pago.Monto);
                cmdHist.Parameters.AddWithValue("@ref", (object?)pago.NumeroReferencia ?? DBNull.Value);
                cmdHist.Parameters.AddWithValue("@not", (object?)pago.Notas ?? DBNull.Value);
                cmdHist.ExecuteNonQuery();
            }

            string sqlUpdateCxp = @"UPDATE CUENTA_PAGAR 
                SET saldo_pendiente = saldo_pendiente - @monto,
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
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    public void AnularCompra(int compraId, int usuarioId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();
        using var trans = conn.BeginTransaction();
        try
        {
            var detalles = new List<(int pId, decimal cant, string refProv)>();
            using (var cmdGet = new SqlCommand("SELECT D.producto_id, D.cantidad, E.numero_factura_proveedor FROM COMPRA_DETALLE D JOIN COMPRA_ENCABEZADO E ON D.compra_id = E.id WHERE E.id = @id AND E.estado = 'Procesada'", conn, trans))
            {
                cmdGet.Parameters.AddWithValue("@id", compraId);
                using var reader = cmdGet.ExecuteReader();
                while (reader.Read()) detalles.Add(((int)reader[0], (decimal)reader[1], reader[2]?.ToString() ?? "S/N"));
            }

            if (detalles.Count == 0) throw new Exception("Compra no encontrada o ya está anulada.");

            foreach (var det in detalles)
            {
                string sqlMov = "INSERT INTO MOVIMIENTO_INVENTARIO (producto_id, usuario_id, fecha, tipo_movimiento, cantidad, documento_referencia, notas) VALUES (@pId, @uId, GETDATE(), 'Salida', @can, @ref, 'ANULACIÓN DE COMPRA')";
                using var cmdMov = new SqlCommand(sqlMov, conn, trans);
                cmdMov.Parameters.AddWithValue("@pId", det.pId);
                cmdMov.Parameters.AddWithValue("@uId", usuarioId);
                cmdMov.Parameters.AddWithValue("@can", det.cant);
                cmdMov.Parameters.AddWithValue("@ref", "ANUL-" + det.refProv);
                cmdMov.ExecuteNonQuery();
            }

            string sqlAnuCxp = "UPDATE CUENTA_PAGAR SET estado = 'Anulado', saldo_pendiente = 0 WHERE compra_id = @cId";
            using var cmdAnuCxp = new SqlCommand(sqlAnuCxp, conn, trans);
            cmdAnuCxp.Parameters.AddWithValue("@cId", compraId);
            cmdAnuCxp.ExecuteNonQuery();

            using (var cmdUp = new SqlCommand("UPDATE COMPRA_ENCABEZADO SET estado = 'Anulada' WHERE id = @id", conn, trans))
            {
                cmdUp.Parameters.AddWithValue("@id", compraId);
                cmdUp.ExecuteNonQuery();
            }

            trans.Commit();
        }
        catch { trans.Rollback(); throw; }
    }

    public List<AlertaPagoDTO> ObtenerAlertasPagos()
    {
        var alertas = new List<AlertaPagoDTO>();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("SELECT * FROM VW_ALERTA_CUENTAS_PAGAR_PROXIMAS ORDER BY fecha_vencimiento ASC", conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            alertas.Add(new AlertaPagoDTO
            {
                Id = (int)reader["id"],
                ProveedorNombre = reader["proveedor_nombre"].ToString()!,
                SaldoPendiente = (decimal)reader["saldo_pendiente"],
                FechaVencimiento = (DateTime)reader["fecha_vencimiento"],
                DiasParaVencimiento = (int)reader["dias_para_vencimiento"],
                Urgencia = reader["urgencia"].ToString()!
            });
        }
        return alertas;
    }

    public List<DeudaConsultaDTO> BuscarDeudas(string filtro)
    {
        var lista = new List<DeudaConsultaDTO>();
        using var conn = new SqlConnection(_connectionString);

        string sql = @"SELECT * FROM VW_CONSULTA_DEUDAS 
                       WHERE ProveedorCedula LIKE @f 
                          OR ProveedorNombre LIKE @f 
                          OR NumeroFactura LIKE @f
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

    // --- NUEVO: RESUMEN GLOBAL DE CXP ---
    public ResumenCxpDTO ObtenerResumenGeneralCxp()
    {
        using var conn = new SqlConnection(_connectionString);
        string sql = @"SELECT 
                        SUM(saldo_pendiente) as TotalGlobal,
                        COUNT(id) as TotalFacturas,
                        COUNT(DISTINCT proveedor_id) as TotalProveedores,
                        MIN(fecha_vencimiento) as ProximaFecha
                       FROM CUENTA_PAGAR 
                       WHERE estado = 'Pendiente' AND saldo_pendiente > 0";

        conn.Open();
        using var cmd = new SqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new ResumenCxpDTO
            {
                TotalDeudaGlobal = reader["TotalGlobal"] != DBNull.Value ? (decimal)reader["TotalGlobal"] : 0,
                FacturasPendientes = reader["TotalFacturas"] != DBNull.Value ? (int)reader["TotalFacturas"] : 0,
                ProveedoresAQuienesSeDebe = reader["TotalProveedores"] != DBNull.Value ? (int)reader["TotalProveedores"] : 0,
                FechaProximoVencimiento = reader["ProximaFecha"] != DBNull.Value ? (DateTime)reader["ProximaFecha"] : null
            };
        }
        return new ResumenCxpDTO();
    }
}