using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Interfaces;

public interface ICompraService
{
    void RegistrarNuevaCompra(CompraDTO compra, UsuarioDTO ejecutor);
    void AbonarAFactura(PagoCxpDTO pago, UsuarioDTO ejecutor);
    List<DeudaConsultaDTO> ListarDeudas(string filtro, UsuarioDTO ejecutor);
    List<PagoHistorialDTO> ListarHistorialDePagos(int compraId);
    ResumenCxpDTO ObtenerResumenCxp(UsuarioDTO ejecutor);
    void AnularCompraExistente(int compraId, UsuarioDTO ejecutor);
    List<AlertaPagoDTO> ListarAlertasPagos(UsuarioDTO ejecutor);
}