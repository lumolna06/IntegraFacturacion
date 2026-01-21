using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

// Cambiamos el constructor para recibir el Factory inyectado
public class CajaService(CajaFactory cajaFactory)
{
    private readonly CajaFactory _cajaFactory = cajaFactory;

    public int AbrirCaja(CajaAperturaDTO apertura) => _cajaFactory.AbrirCaja(apertura);

    public void CerrarCaja(CajaCierreDTO cierre)
    {
        if (cierre.MontoRealEnCaja < 0)
            throw new Exception("El monto real no puede ser un valor negativo.");

        _cajaFactory.CerrarCaja(cierre);
    }

    public List<object> ObtenerHistorial()
    {
        return _cajaFactory.ObtenerHistorialCierres();
    }
}