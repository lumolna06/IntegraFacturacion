using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface ICategoriaService
{
    ApiResponse<List<CategoriaDTO>> ObtenerTodas();

    // Actualizado: Se añade el parámetro ejecutor para validación de roles
    ApiResponse<bool> Crear(CategoriaDTO categoria, UsuarioDTO ejecutor);
}