using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class CategoriaService(CategoriaFactory factory) : ICategoriaService
{
    private readonly CategoriaFactory _factory = factory;

    public ApiResponse<List<CategoriaDTO>> ObtenerTodas()
    {
        try
        {
            var lista = _factory.GetAll();
            // Result = true, Message = texto, Data = lista
            return new ApiResponse<List<CategoriaDTO>>(true, "Lista de categorías", lista);
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
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
                return new ApiResponse<bool>(false, "El nombre de la categoría es obligatorio.");

            // Pasamos el ejecutor al factory para la validación de roles
            bool ok = _factory.Create(categoria, ejecutor);

            return new ApiResponse<bool>(ok, ok ? "Categoría guardada con éxito" : "No se pudo guardar la categoría", ok);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Capturamos el error de permisos del Factory
            return new ApiResponse<bool>(false, ex.Message, false);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, $"Error en el servicio de categorías: {ex.Message}", false);
        }
    }
}