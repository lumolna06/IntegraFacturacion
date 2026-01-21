using System.Xml.Linq;
using System.Globalization;
using Microsoft.Data.SqlClient;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class XmlParserService(string connectionString)
{
    private readonly string _connectionString = connectionString;

    public XmlPreprocesoDTO ProcesarFacturaCR(string xmlContent)
    {
        XDocument doc = XDocument.Parse(xmlContent);
        XNamespace ns = doc.Root.GetDefaultNamespace();

        var emisor = doc.Descendants(ns + "Emisor").FirstOrDefault();

        // Extracción de datos básicos
        var cedulaXml = emisor?.Element(ns + "Identificacion")?.Element(ns + "Numero")?.Value;
        var nombreXml = emisor?.Element(ns + "Nombre")?.Value;

        // Extracción de contacto
        var nodoTel = emisor?.Element(ns + "Telefono");
        var telefonoXml = nodoTel != null
            ? $"{nodoTel.Element(ns + "CodigoPais")?.Value}{nodoTel.Element(ns + "NumTelefono")?.Value}"
            : null;
        var correoXml = emisor?.Element(ns + "CorreoElectronico")?.Value;

        // 1. Garantizar proveedor con datos de contacto
        int proveedorIdInterno = ObtenerORegistrarProveedor(cedulaXml, nombreXml, telefonoXml, correoXml);

        var resultado = new XmlPreprocesoDTO
        {
            NumeroFactura = doc.Descendants(ns + "NumeroConsecutivo").FirstOrDefault()?.Value,
            FechaEmision = DateTime.TryParse(doc.Descendants(ns + "FechaEmision").FirstOrDefault()?.Value, out var fecha) ? fecha : DateTime.Now,
            ProveedorCedula = cedulaXml,
            ProveedorNombre = nombreXml,
            ProveedorId = proveedorIdInterno
        };

        foreach (var linea in doc.Descendants(ns + "LineaDetalle"))
        {
            string cabys = linea.Element(ns + "Codigo")?.Value
                          ?? linea.Element(ns + "CodigoCABYS")?.Value
                          ?? "";

            var sugerencia = BuscarEquivalencia(proveedorIdInterno, cabys);

            resultado.Lineas.Add(new LineaPreprocesoDTO
            {
                DetalleXml = linea.Element(ns + "Detalle")?.Value,
                CodigoCabys = cabys,
                Cantidad = decimal.Parse(linea.Element(ns + "Cantidad")?.Value ?? "0", CultureInfo.InvariantCulture),
                PrecioUnitario = decimal.Parse(linea.Element(ns + "PrecioUnitario")?.Value ?? "0", CultureInfo.InvariantCulture),
                MontoImpuesto = decimal.Parse(linea.Descendants(ns + "Monto").FirstOrDefault()?.Value ?? "0", CultureInfo.InvariantCulture),
                TotalLinea = decimal.Parse(linea.Element(ns + "MontoTotalLinea")?.Value ?? "0", CultureInfo.InvariantCulture),
                ProductoIdSugerido = sugerencia.id,
                ProductoNombreSugerido = sugerencia.nombre
            });
        }
        return resultado;
    }

    private int ObtenerORegistrarProveedor(string? cedula, string? nombre, string? telefono, string? correo)
    {
        if (string.IsNullOrEmpty(cedula)) return 0;

        string cedulaLimpia = cedula.Replace("-", "").Trim();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        string sqlBusqueda = "SELECT id FROM PROVEEDOR WHERE REPLACE(identificacion, '-', '') = @ced";
        using (var cmdBusqueda = new SqlCommand(sqlBusqueda, conn))
        {
            cmdBusqueda.Parameters.AddWithValue("@ced", cedulaLimpia);
            var result = cmdBusqueda.ExecuteScalar();
            if (result != null) return Convert.ToInt32(result);
        }

        // Insertar con Teléfono y Correo (Asegúrate de que estas columnas existan en tu tabla)
        string sqlInsert = @"INSERT INTO PROVEEDOR (nombre, identificacion, telefono, correo, activo) 
                             VALUES (@nom, @ced, @tel, @eml, 1);
                             SELECT SCOPE_IDENTITY();";

        using (var cmdInsert = new SqlCommand(sqlInsert, conn))
        {
            cmdInsert.Parameters.AddWithValue("@nom", nombre ?? "Proveedor Nuevo (XML)");
            cmdInsert.Parameters.AddWithValue("@ced", cedulaLimpia);
            cmdInsert.Parameters.AddWithValue("@tel", (object?)telefono ?? DBNull.Value);
            cmdInsert.Parameters.AddWithValue("@eml", (object?)correo ?? DBNull.Value);
            return Convert.ToInt32(cmdInsert.ExecuteScalar());
        }
    }

    private (int? id, string? nombre) BuscarEquivalencia(int proveedorId, string cabys)
    {
        if (proveedorId == 0 || string.IsNullOrEmpty(cabys)) return (null, null);

        using var conn = new SqlConnection(_connectionString);
        string sql = @"SELECT P.id, P.nombre 
                       FROM PRODUCTO_EQUIVALENCIA E 
                       INNER JOIN PRODUCTO P ON E.producto_id = P.id 
                       WHERE E.proveedor_id = @provId AND E.codigo_xml = @cabys";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@provId", proveedorId);
        cmd.Parameters.AddWithValue("@cabys", cabys);

        try
        {
            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (Convert.ToInt32(reader["id"]), reader["nombre"].ToString());
            }
        }
        catch { }

        return (null, null);
    }
}