using IntegraPro.DataAccess.Factory;
using IntegraPro.DTO.Models;
using System.Collections.Generic;

namespace IntegraPro.AppLogic.Services;

// El constructor ahora recibe ConfiguracionFactory para obtener los datos de la empresa
public class ProformaService(ProformaFactory factory, ConfiguracionFactory configFactory)
{
    // Métodos de Proforma
    public int GuardarProforma(ProformaEncabezadoDTO p) => factory.CrearProforma(p);

    public List<ProformaEncabezadoDTO> Consultar(string filtro) => factory.ListarProformas(filtro);

    public ProformaEncabezadoDTO ObtenerPorId(int id) => factory.ObtenerPorId(id);

    public List<ProformaEncabezadoDTO> ObtenerPorCliente(int clienteId) => factory.ListarPorCliente(clienteId);

    public void EditarProforma(ProformaEncabezadoDTO p) => factory.ActualizarProforma(p);

    public void Anular(int id) => factory.AnularProforma(id);

    public string Facturar(int id, int usuarioId, string medio) => factory.ConvertirAFactura(id, usuarioId, medio);

    // --- MÉTODO ACTUALIZADO PARA USAR CONFIGURACIONFACTORY ---
    public EmpresaDTO? ObtenerEmpresa()
    {
        // Llamamos al método que acabamos de crear en ConfiguracionFactory
        return configFactory.ObtenerEmpresa();
    }
}