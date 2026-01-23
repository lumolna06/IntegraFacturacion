using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegraPro.DTO.Models;

public class EmpresaDTO
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty; // Agregado
    public string CedulaJuridica { get; set; } = string.Empty;
    public string TipoRegimen { get; set; } = "Tradicional";
    public string Telefono { get; set; } = string.Empty; // Agregado
    public string CorreoNotificaciones { get; set; } = string.Empty;
    public string SitioWeb { get; set; } = string.Empty; // Agregado
    public string? Logo { get; set; } // Agregado (Nombre del archivo o ruta)
}

public class LicenciaDTO
{
    public string LicenciaKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = "Demo";
}
