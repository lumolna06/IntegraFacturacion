using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Interfaces;

public interface IClienteService
{
    // Recibe ejecutor para validar permiso de lectura del módulo
    ApiResponse<List<ClienteDTO>> ObtenerTodos(UsuarioDTO ejecutor);

    // Devuelve el ID generado del nuevo cliente
    ApiResponse<int> Crear(ClienteDTO cliente, UsuarioDTO ejecutor);

    // Actualización de datos existentes
    ApiResponse<bool> Actualizar(ClienteDTO cliente, UsuarioDTO ejecutor);

    // Búsqueda rápida (comúnmente usada en ventas)
    // CAMBIO: Ahora requiere el ejecutor para validar que quien busca tiene permisos
    ApiResponse<ClienteDTO> BuscarPorIdentificacion(string identificacion, UsuarioDTO ejecutor);
}