using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class CompraService(CompraFactory compraFactory)
{
    private readonly CompraFactory _factory = compraFactory;

    public void RegistrarNuevaCompra(CompraDTO compra)
    {
        if (compra.Detalles.Count == 0)
            throw new Exception("No hay productos en la compra.");

        // Solo llamamos a ProcesarCompra. 
        // El Factory ahora se encarga internamente de las equivalencias 
        // dentro de la misma transacción SQL.
        _factory.ProcesarCompra(compra);
    }

    public void AnularCompraExistente(int compraId, int usuarioId)
    {
        _factory.AnularCompra(compraId, usuarioId);
    }

    public List<AlertaPagoDTO> ListarAlertasPagos()
    {
        return _factory.ObtenerAlertasPagos();
    }
}