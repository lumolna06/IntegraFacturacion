using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class CategoriaService(CategoriaFactory factory) : ICategoriaService
{
    private readonly CategoriaFactory _factory = factory;

    // ACTUALIZADO: Ahora recibe UsuarioDTO para validar acceso preventivo
    public ApiResponse<List<CategoriaDTO>> ObtenerTodas(UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("inventario");

            var lista = _factory.GetAll(ejecutor);
            return new ApiResponse<List<CategoriaDTO>>(true, "Lista de categorías", lista);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ApiResponse<List<CategoriaDTO>>(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<CategoriaDTO>>(false, $"Error al obtener categorías: {ex.Message}");
        }
    }

    public ApiResponse<bool> Crear(CategoriaDTO categoria, UsuarioDTO ejecutor)
    {
        try
        {
            // --- SEGURIDAD: Validación preventiva ---
            ejecutor.ValidarAcceso("inventario");
            ejecutor.ValidarEscritura();

            // Lógica original: Validaciones de negocio
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
                return new ApiResponse<bool>(false, "El nombre de la categoría es obligatorio.");

            bool ok = _factory.Create(categoria, ejecutor);

            return new ApiResponse<bool>(ok, ok ? "Categoría guardada con éxito" : "No se pudo guardar la categoría", ok);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Captura los errores de permisos (tanto del Service como del Factory)
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error en el servicio de categorías: {ex.Message}", false);
        }
    }
}