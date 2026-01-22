using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Services;

public class ProformaService(ProformaFactory factory)
{
    // Crear una nueva proforma
    public int GuardarProforma(ProformaEncabezadoDTO p) => factory.CrearProforma(p);

    // Listado general con filtro por nombre o estado
    public List<ProformaEncabezadoDTO> Consultar(string filtro) => factory.ListarProformas(filtro);

    // Listado específico para un cliente
    public List<ProformaEncabezadoDTO> ObtenerPorCliente(int clienteId) => factory.ListarPorCliente(clienteId);

    // Modificar una proforma existente
    public void EditarProforma(ProformaEncabezadoDTO p) => factory.ActualizarProforma(p);

    // Proceso de conversión a factura (descuenta stock mediante Trigger)
    public string Facturar(int id, int usuarioId, string medio) => factory.ConvertirAFactura(id, usuarioId, medio);
}