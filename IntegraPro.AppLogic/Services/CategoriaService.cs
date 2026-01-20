using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class CategoriaService(CategoriaFactory factory) : ICategoriaService
{
    public ApiResponse<List<CategoriaDTO>> ObtenerTodas()
        => new(true, "Lista de categorías", factory.GetAll());

    public ApiResponse<bool> Crear(CategoriaDTO categoria)
    {
        try
        {
            bool ok = factory.Create(categoria);
            return new ApiResponse<bool>(ok, ok ? "Guardado con éxito" : "Error", ok);
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}