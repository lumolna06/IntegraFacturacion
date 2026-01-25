using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using IntegraPro.AppLogic.Interfaces; // Agregado para la interfaz
using System;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Services;

public class CompraService(CompraFactory compraFactory) : ICompraService
{
    private readonly CompraFactory _factory = compraFactory;

    public void RegistrarNuevaCompra(CompraDTO compra, UsuarioDTO ejecutor)
    {
        if (compra.Detalles.Count == 0)
            throw new Exception("No hay productos en la compra.");

        // Pasamos el ejecutor para que el Factory valide permisos 'compras' 
        // y asigne la sucursal correcta si tiene 'sucursal_limit'.
        _factory.ProcesarCompra(compra, ejecutor);
    }

    public void AbonarAFactura(PagoCxpDTO pago, UsuarioDTO ejecutor)
    {
        if (pago.Monto <= 0)
            throw new Exception("El monto del abono debe ser mayor a cero.");

        if (pago.CompraId <= 0)
            throw new Exception("Debe especificar una compra válida para aplicar el pago.");

        // El Factory usará el ID del ejecutor para el historial de pagos.
        _factory.RegistrarPagoCxp(pago, ejecutor);
    }

    public List<DeudaConsultaDTO> ListarDeudas(string filtro, UsuarioDTO ejecutor)
    {
        // CORRECCIÓN: Se agrega 'ejecutor' para cumplir con la firma del Factory
        // y permitir el filtrado de seguridad por sucursal.
        return _factory.BuscarDeudas(filtro?.Trim() ?? "", ejecutor);
    }

    public List<PagoHistorialDTO> ListarHistorialDePagos(int compraId)
    {
        if (compraId <= 0)
            throw new Exception("El ID de la compra proporcionado no es válido.");

        return _factory.ObtenerHistorialPagos(compraId);
    }

    public ResumenCxpDTO ObtenerResumenCxp(UsuarioDTO ejecutor)
    {
        // El resumen ahora será filtrado por sucursal automáticamente si el ejecutor tiene límites.
        return _factory.ObtenerResumenGeneralCxp(ejecutor);
    }

    public void AnularCompraExistente(int compraId, UsuarioDTO ejecutor)
    {
        // Ya no pasamos un simple usuarioId, sino el DTO completo para validar 
        // si tiene permiso de anular en la sucursal de la compra.
        _factory.AnularCompra(compraId, ejecutor);
    }

    public List<AlertaPagoDTO> ListarAlertasPagos(UsuarioDTO ejecutor)
    {
        return _factory.ObtenerAlertasPagos(ejecutor);
    }
}