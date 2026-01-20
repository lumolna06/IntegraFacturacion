using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface ICategoriaService
{
    ApiResponse<List<CategoriaDTO>> ObtenerTodas();
    ApiResponse<bool> Crear(CategoriaDTO categoria);
}