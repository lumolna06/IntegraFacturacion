namespace IntegraPro.DTO.Models
{
    public class PermisosDetalle
    {
        public bool Ventas { get; set; }
        public bool Compras { get; set; }
        public bool Inventario { get; set; }
        public bool Caja { get; set; }
        public bool Reportes { get; set; }
        public bool Clientes { get; set; }
        public bool Sucursal_Limit { get; set; } // La llave para filtrar por sucursal
        public bool All { get; set; } // Para el Administrador
    }
}