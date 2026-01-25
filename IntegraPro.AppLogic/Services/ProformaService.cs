using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

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
            // Validamos acceso general y que no sea solo lectura
            ejecutor.ValidarAcceso("proformas");
            ejecutor.ValidarEscritura();

            int id = _factory.CrearProforma(p, ejecutor);
            return new ApiResponse<int>(true, "Proforma generada correctamente", id);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<int>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<int>(false, $"Error al guardar: {ex.Message}"); }
    }

    public ApiResponse<List<ProformaEncabezadoDTO>> Consultar(string filtro, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proformas");
            var lista = _factory.ListarProformas(filtro, ejecutor);
            return new ApiResponse<List<ProformaEncabezadoDTO>>(true, "Consulta exitosa", lista);
        }
        catch (Exception ex) { return new ApiResponse<List<ProformaEncabezadoDTO>>(false, ex.Message); }
    }

    public ApiResponse<ProformaEncabezadoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proformas");
            var p = _factory.ObtenerPorId(id, ejecutor);

            return p != null
                ? new ApiResponse<ProformaEncabezadoDTO>(true, "Ok", p)
                : new ApiResponse<ProformaEncabezadoDTO>(false, "Proforma no encontrada o sin acceso.");
        }
        catch (Exception ex) { return new ApiResponse<ProformaEncabezadoDTO>(false, ex.Message); }
    }

    public ApiResponse<List<ProformaEncabezadoDTO>> ObtenerPorCliente(int clienteId, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proformas");
            // Filtramos la lista general por cliente
            var lista = _factory.ListarProformas(string.Empty, ejecutor)
                                .Where(x => x.ClienteId == clienteId).ToList();

            return new ApiResponse<List<ProformaEncabezadoDTO>>(true, "Ok", lista);
        }
        catch (Exception ex) { return new ApiResponse<List<ProformaEncabezadoDTO>>(false, ex.Message); }
    }

    public ApiResponse<bool> Anular(int id, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proformas");
            ejecutor.ValidarEscritura();

            _factory.AnularProforma(id, ejecutor);
            return new ApiResponse<bool>(true, "Proforma anulada correctamente", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message); }
    }

    public ApiResponse<string> Facturar(int id, string medio, UsuarioDTO ejecutor)
    {
        try
        {
            // 1. Seguridad de doble nivel (Acceso a ventas + No ser solo lectura)
            ejecutor.ValidarAcceso("ventas");
            ejecutor.ValidarEscritura();

            // 2. Validación preventiva de stock (Fail-Fast)
            var empresa = _configFactory.ObtenerEmpresa(ejecutor);
            bool permitirNegativo = empresa?.PermitirStockNegativo ?? false;

            if (!permitirNegativo)
            {
                var proforma = _factory.ObtenerPorId(id, ejecutor);
                if (proforma == null) return new ApiResponse<string>(false, "La proforma no existe.");

                foreach (var det in proforma.Detalles)
                {
                    // Validamos stock específicamente en la sucursal del ejecutor
                    var producto = _prodFactory.GetById(det.ProductoId, ejecutor);
                    if (producto != null && producto.Existencia < det.Cantidad)
                    {
                        return new ApiResponse<string>(false,
                            $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Existencia:N2}");
                    }
                }
            }

            // 3. Conversión atómica en Base de Datos
            string consecutivo = _factory.ConvertirAFactura(id, medio, ejecutor);
            return new ApiResponse<string>(true, $"Factura {consecutivo} generada con éxito.", consecutivo);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<string>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<string>(false, $"Error en facturación: {ex.Message}"); }
    }
}