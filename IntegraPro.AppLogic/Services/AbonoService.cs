using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;

namespace IntegraPro.AppLogic.Services;

public class AbonoService(string connectionString)
{
    private readonly AbonoFactory _abonoFactory = new(connectionString);

    public void RegistrarAbono(AbonoDTO abono, int clienteId)
    {
        if (abono.MontoAbonado <= 0)
            throw new Exception("El monto del abono debe ser mayor a cero.");

        // Opcional: Podrías validar aquí que el monto no exceda el saldo de la cuenta
        // antes de llamar al factory.

        _abonoFactory.ProcesarAbonoCompleto(abono, clienteId);
    }

    // --- NUEVO MÉTODO PARA CONSULTAR PENDIENTES ---
    public List<object> ObtenerPendientes(int? clienteId)
    {
        return _abonoFactory.ObtenerFacturasPendientes(clienteId);
    }
}