using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

/// <summary>
/// Implementación de la lógica de negocio para Proformas.
/// Implementa IProformaService para permitir la Inyección de Dependencias.
/// </summary>
public class ProformaService(
    ProformaFactory factory,
    ConfiguracionFactory configFactory,
    ProductoFactory prodFactory) : IProformaService
{
    private readonly ProformaFactory _factory = factory;
    private readonly ConfiguracionFactory _configFactory = configFactory;
    private readonly ProductoFactory _prodFactory = prodFactory;

    public ApiResponse<int> GuardarProforma(ProformaEncabezadoDTO p, UsuarioDTO ejecutor)
    {
        try
        {
            if (ejecutor.TienePermiso("solo_lectura"))
                return new ApiResponse<int>(false, "Su perfil de solo lectura no permite crear proformas.");

            int id = _factory.CrearProforma(p, ejecutor);
            return new ApiResponse<int>(true, "Proforma generada correctamente", id);
        }
        catch (Exception ex) { return new ApiResponse<int>(false, ex.Message); }
    }

    public ApiResponse<List<ProformaEncabezadoDTO>> Consultar(string filtro, UsuarioDTO ejecutor)
    {
        try
        {
            var lista = _factory.ListarProformas(filtro, ejecutor);
            return new ApiResponse<List<ProformaEncabezadoDTO>>(true, "Consulta exitosa", lista);
        }
        catch (Exception ex) { return new ApiResponse<List<ProformaEncabezadoDTO>>(false, ex.Message); }
    }

    public ApiResponse<ProformaEncabezadoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        try
        {
            var p = _factory.ObtenerPorId(id, ejecutor);
            return p != null
                ? new ApiResponse<ProformaEncabezadoDTO>(true, "Ok", p)
                : new ApiResponse<ProformaEncabezadoDTO>(false, "Proforma no encontrada.");
        }
        catch (Exception ex) { return new ApiResponse<ProformaEncabezadoDTO>(false, ex.Message); }
    }

    public ApiResponse<List<ProformaEncabezadoDTO>> ObtenerPorCliente(int clienteId, UsuarioDTO ejecutor)
    {
        try
        {
            var lista = _factory.ListarPorCliente(clienteId, ejecutor);
            return new ApiResponse<List<ProformaEncabezadoDTO>>(true, "Lista de proformas del cliente recuperada.", lista);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ProformaEncabezadoDTO>>(false, $"Error al obtener proformas del cliente: {ex.Message}");
        }
    }

    public ApiResponse<bool> Anular(int id, UsuarioDTO ejecutor)
    {
        try
        {
            _factory.AnularProforma(id, ejecutor);
            return new ApiResponse<bool>(true, "Proforma anulada correctamente", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message); }
    }

    public ApiResponse<string> Facturar(int id, string medio, UsuarioDTO ejecutor)
    {
        try
        {
            if (!ejecutor.TienePermiso("ventas"))
                return new ApiResponse<string>(false, "No tiene permisos de ventas para facturar.");

            // VALIDACIÓN PREVENTIVA DE STOCK
            var empresa = _configFactory.ObtenerEmpresa();
            bool permitirNegativo = empresa?.PermitirStockNegativo ?? false;

            if (!permitirNegativo)
            {
                var proforma = _factory.ObtenerPorId(id, ejecutor);
                if (proforma == null) return new ApiResponse<string>(false, "La proforma no existe.");

                foreach (var det in proforma.Detalles)
                {
                    var producto = _prodFactory.GetById(det.ProductoId, ejecutor);
                    if (producto != null && producto.Existencia < det.Cantidad)
                    {
                        return new ApiResponse<string>(false, $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Existencia:N2}");
                    }
                }
            }

            string consecutivo = _factory.ConvertirAFactura(id, ejecutor.Id, medio, ejecutor);
            return new ApiResponse<string>(true, $"Factura generada: {consecutivo}", consecutivo);
        }
        catch (Exception ex) { return new ApiResponse<string>(false, ex.Message); }
    }
}