using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ProveedorService(ProveedorFactory factory) : IProveedorService
{
    public ApiResponse<List<ProveedorDTO>> ObtenerTodos(UsuarioDTO ejecutor)
    {
        try
        {
            var lista = factory.ObtenerTodos(ejecutor);
            return new ApiResponse<List<ProveedorDTO>>(true, "Proveedores cargados", lista);
        }
        catch (Exception ex) { return new ApiResponse<List<ProveedorDTO>>(false, ex.Message); }
    }

    public ApiResponse<bool> Crear(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        try
        {
            factory.Crear(proveedor, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor creado con éxito", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, "Error interno: " + ex.Message, false); }
    }

    public ApiResponse<bool> Actualizar(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        try
        {
            factory.Actualizar(proveedor, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor actualizado", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message, false); }
    }

    public ApiResponse<bool> Eliminar(int id, UsuarioDTO ejecutor)
    {
        try
        {
            factory.Eliminar(id, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor desactivado", true);
        }
        catch (Exception ex) { return new ApiResponse<bool>(false, ex.Message, false); }
    }
}