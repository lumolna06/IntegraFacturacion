using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Utils;
using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class ClienteService(ClienteFactory cliFactory) : IClienteService
{
    private readonly ClienteFactory _cliFactory = cliFactory;

    public ApiResponse<List<ClienteDTO>> ObtenerTodos(UsuarioDTO ejecutor)
    {
        try
        {
            // Tu Factory no pide parámetros aquí, así que lo llamamos limpio
            var clientes = _cliFactory.ObtenerTodos();
            return new ApiResponse<List<ClienteDTO>>(true, "Clientes recuperados con éxito", clientes);
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ClienteDTO>>(false, $"Error al listar clientes: {ex.Message}");
        }
    }

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

    // Adaptado: Se llama 'Crear' para la Interfaz pero usa '_cliFactory.Insertar'
    public ApiResponse<int> Crear(ClienteDTO cliente, UsuarioDTO ejecutor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cliente.Identificacion))
                return new ApiResponse<int>(false, "La identificación es obligatoria.");

            // Usamos tu método Insertar que devuelve el int
            int nuevoId = _cliFactory.Insertar(cliente, ejecutor);

            return new ApiResponse<int>(true, "Cliente registrado correctamente", nuevoId);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<int>(false, ex.Message); }
        catch (Exception ex) { return new ApiResponse<int>(false, $"Error: {ex.Message}"); }
    }

    public ApiResponse<bool> Actualizar(ClienteDTO cliente, UsuarioDTO ejecutor)
    {
        try
        {
            if (cliente.Id <= 0)
                return new ApiResponse<bool>(false, "ID de cliente no válido.", false);

            bool exito = _cliFactory.Actualizar(cliente, ejecutor);
            return new ApiResponse<bool>(true, "Cliente actualizado correctamente", exito);
        }
        catch (UnauthorizedAccessException ex) { return new ApiResponse<bool>(false, ex.Message, false); }
        catch (Exception ex) { return new ApiResponse<bool>(false, $"Error: {ex.Message}", false); }
    }
}