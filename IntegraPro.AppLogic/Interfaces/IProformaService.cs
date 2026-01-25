using IntegraPro.AppLogic.Utils;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Interfaces;

public interface IProformaService
{
    ApiResponse<int> GuardarProforma(ProformaEncabezadoDTO p, UsuarioDTO ejecutor);
    ApiResponse<List<ProformaEncabezadoDTO>> Consultar(string filtro, UsuarioDTO ejecutor);
    ApiResponse<ProformaEncabezadoDTO> ObtenerPorId(int id, UsuarioDTO ejecutor);
    ApiResponse<List<ProformaEncabezadoDTO>> ObtenerPorCliente(int clienteId, UsuarioDTO ejecutor);
    ApiResponse<bool> Anular(int id, UsuarioDTO ejecutor);
    ApiResponse<string> Facturar(int id, string medio, UsuarioDTO ejecutor);
}