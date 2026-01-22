using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

// Cambiamos a constructor primario para inyección de dependencias
public class AbonoService(AbonoFactory abonoFactory)
{
    private readonly AbonoFactory _factory = abonoFactory;

    public void RegistrarAbono(AbonoDTO abono, int clienteId)
    {
        if (abono.MontoAbonado <= 0)
            throw new Exception("El monto del abono debe ser mayor a cero.");

        _factory.ProcesarAbonoCompleto(abono, clienteId);
    }

    public List<object> ObtenerPendientes(int? clienteId)
    {
        return _factory.ObtenerFacturasPendientes(clienteId);
    }

    // --- NUEVOS MÉTODOS ---

    public List<CxcConsultaDTO> BuscarCuentasPorCobrar(string filtro)
    {
        // Filtro puede ser nombre, cédula, correo o teléfono
        return _factory.BuscarCxcClientes(filtro?.Trim() ?? "");
    }

    public List<AbonoHistorialDTO> ObtenerHistorialDeAbonos(int facturaId)
    {
        if (facturaId <= 0) throw new Exception("ID de factura no válido.");
        return _factory.ListarHistorialAbonos(facturaId);
    }

    public List<AlertaCxcDTO> ListarAlertasMora()
    {
        return _factory.ObtenerAlertasVencimiento();
    }

    public ResumenCxcDTO ObtenerResumenGeneral()
    {
        return _factory.ObtenerTotalesCxc();
    }
}