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

        // El Factory maneja la transacción: Compra, CXP, Inventarios y Equivalencias
        _factory.ProcesarCompra(compra);
    }

    public void AbonarAFactura(PagoCxpDTO pago)
    {
        if (pago.Monto <= 0)
            throw new Exception("El monto del abono debe ser mayor a cero.");

        if (pago.CompraId <= 0)
            throw new Exception("Debe especificar una compra válida para aplicar el pago.");

        _factory.RegistrarPagoCxp(pago);
    }

    public List<DeudaConsultaDTO> ListarDeudas(string filtro)
    {
        return _factory.BuscarDeudas(filtro?.Trim() ?? "");
    }

    public List<PagoHistorialDTO> ListarHistorialDePagos(int compraId)
    {
        if (compraId <= 0)
            throw new Exception("El ID de la compra proporcionado no es válido.");

        return _factory.ObtenerHistorialPagos(compraId);
    }

    // --- NUEVO MÉTODO: RESUMEN GLOBAL PARA DASHBOARD ---
    public ResumenCxpDTO ObtenerResumenCxp()
    {
        return _factory.ObtenerResumenGeneralCxp();
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