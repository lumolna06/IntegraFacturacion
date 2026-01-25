using Microsoft.AspNetCore.Mvc;
using IntegraPro.AppLogic.Interfaces;
using IntegraPro.DTO.Models;
using System.Security.Claims;
using System.Text.Json;

namespace IntegraPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompraController(ICompraService compraService) : ControllerBase
{
    private readonly ICompraService _service = compraService;

    private UsuarioDTO UsuarioActual
    {
        get
        {
            // 1. Intentamos obtener los datos del Token
            var identity = User.Identity as ClaimsIdentity;

            var usuario = new UsuarioDTO
            {
                Id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0"),
                RolId = int.Parse(User.FindFirstValue("RolId") ?? "0"),
                SucursalId = int.Parse(User.FindFirstValue("SucursalId") ?? "0"),
                Username = User.Identity?.Name ?? "Invitado"
            };

            // 2. Intentamos cargar los permisos desde el JSON del Token
            var permisosJson = User.FindFirstValue("Permisos");
            if (!string.IsNullOrEmpty(permisosJson))
            {
                usuario.PermisosJson = permisosJson;
                try
                {
                    usuario.Permisos = JsonSerializer.Deserialize<Dictionary<string, bool>>(permisosJson)
                                       ?? new Dictionary<string, bool>();
                }
                catch
                {
                    usuario.Permisos = new Dictionary<string, bool>();
                }
            }

            // === LÍNEA DE EMERGENCIA ===
            // Si el token dice que eres Rol 1 (Admin), forzamos el permiso "all" 
            // Esto descarta problemas de lectura del JSON.
            if (usuario.RolId == 1)
            {
                usuario.Permisos["all"] = true;
            }

            return usuario;
        }
    }

    [HttpPost]
    public IActionResult Post(CompraDTO compra)
    {
        try
        {
            // Verificación visual en la consola de depuración
            Console.WriteLine($"Procesando compra - Usuario: {UsuarioActual.Username}, Rol: {UsuarioActual.RolId}, Admin: {UsuarioActual.TienePermiso("all")}");

            _service.RegistrarNuevaCompra(compra, UsuarioActual);
            return Ok(new { mensaje = "Compra procesada exitosamente." });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Capturamos específicamente el error de permisos para dar más detalle
            return StatusCode(403, new { error = ex.Message, detalle = "El Factory rechazó los permisos del usuario." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("pagar")]
    public IActionResult PagarFactura([FromBody] PagoCxpDTO pago)
    {
        try
        {
            _service.AbonarAFactura(pago, UsuarioActual);
            return Ok(new { mensaje = "Pago registrado exitosamente." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("resumen-general")]
    public IActionResult GetResumenGeneral()
    {
        try
        {
            var resumen = _service.ObtenerResumenCxp(UsuarioActual);
            return Ok(resumen);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("buscar-deudas")]
    public IActionResult GetDeudas([FromQuery] string? filtro = null)
    {
        try
        {
            var deudas = _service.ListarDeudas(filtro ?? "", UsuarioActual);
            return Ok(deudas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{compraId}/historial-pagos")]
    public IActionResult GetHistorialPagos(int compraId)
    {
        try
        {
            var historial = _service.ListarHistorialDePagos(compraId);
            return Ok(historial);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public IActionResult Anular(int id)
    {
        try
        {
            _service.AnularCompraExistente(id, UsuarioActual);
            return Ok(new { mensaje = "Compra anulada y stock revertido." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("alertas-pagos")]
    public IActionResult GetAlertas()
    {
        try
        {
            var alertas = _service.ListarAlertasPagos(UsuarioActual);
            return Ok(alertas);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}