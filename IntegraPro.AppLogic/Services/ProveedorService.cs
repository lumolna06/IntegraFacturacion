using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ProveedorService(ProveedorFactory factory) : IProveedorService
{
    private readonly ProveedorFactory _factory = factory;

    public ApiResponse<List<ProveedorDTO>> ObtenerTodos(UsuarioDTO ejecutor)
    {
        try
        {
            // SEGURIDAD: Validación preventiva en la capa de servicio
            ejecutor.ValidarAcceso("proveedores");

            var lista = _factory.ObtenerTodos(ejecutor);
            return new ApiResponse<List<ProveedorDTO>>(true, "Proveedores cargados con éxito", lista);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<ProveedorDTO>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ProveedorDTO>>(false, "Error al listar proveedores: " + ex.Message);
        }
    }

    public ApiResponse<bool> Crear(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        try
        {
            // SEGURIDAD: Doble validación (Acceso al módulo y permiso de escritura)
            ejecutor.ValidarAcceso("proveedores");
            ejecutor.ValidarEscritura();

            _factory.Crear(proveedor, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor creado con éxito", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, "Error interno: " + ex.Message, false); }
    }

    public ApiResponse<bool> Actualizar(ProveedorDTO proveedor, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proveedores");
            ejecutor.ValidarEscritura();

            _factory.Actualizar(proveedor, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor actualizado con éxito", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, "Error al actualizar: " + ex.Message, false); }
    }

    public ApiResponse<bool> Eliminar(int id, UsuarioDTO ejecutor)
    {
        try
        {
            ejecutor.ValidarAcceso("proveedores");
            ejecutor.ValidarEscritura();

            _factory.Eliminar(id, ejecutor);
            return new ApiResponse<bool>(true, "Proveedor desactivado correctamente", true);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, "Error al eliminar: " + ex.Message, false); }
    }
}