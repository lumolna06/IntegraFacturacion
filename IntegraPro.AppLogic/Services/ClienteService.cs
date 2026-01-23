using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ClienteService(ClienteFactory cliFactory)
{
    private readonly ClienteFactory _cliFactory = cliFactory;

    public ApiResponse<ClienteDTO> BuscarPorIdentificacion(string identificacion)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return new ApiResponse<ClienteDTO>(false, "La identificación no puede estar vacía.");

            var cliente = _cliFactory.ObtenerPorIdentificacion(identificacion);

            if (cliente == null)
                return new ApiResponse<ClienteDTO>(false, "Cliente no encontrado.");

            return new ApiResponse<ClienteDTO>(true, "Cliente localizado", cliente);
        }
        catch (Exception ex)
        {
            return new ApiResponse<ClienteDTO>(false, $"Error al buscar cliente: {ex.Message}");
        }
    }
}