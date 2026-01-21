using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Services;
using IntegraPro.DataAccess.Factory;
using Microsoft.OpenApi.Models;
using IntegraPro.API.Filters;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE CONEXIÓN
// ==========================================
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-AR7JSQE\\SQLEXPRESS;Database=ERP_SistemaPro;Integrated Security=SSPI;TrustServerCertificate=True;";

// ==========================================
// 2. CONFIGURACIÓN DE CORS (Agregado para evitar bloqueos)
// ==========================================
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ==========================================
// 3. INYECCIÓN DE DEPENDENCIAS (N-Capas)
// ==========================================

// --- Capa de Datos (Factories) ---
builder.Services.AddScoped(sp => new UsuarioFactory(connectionString));
builder.Services.AddScoped(sp => new ProductoFactory(connectionString));
builder.Services.AddScoped(sp => new CategoriaFactory(connectionString));
builder.Services.AddScoped(sp => new ConfiguracionFactory(connectionString));
builder.Services.AddScoped(sp => new InventarioFactory(connectionString));
builder.Services.AddScoped(sp => new VentaFactory(connectionString));
builder.Services.AddScoped(sp => new CajaFactory(connectionString));
builder.Services.AddScoped(sp => new ClienteFactory(connectionString));

// --- Capa de Lógica (Services) ---
builder.Services.AddScoped<LicenciaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();

// Aquí el contenedor inyectará automáticamente VentaFactory, ClienteFactory y ProductoFactory
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<CajaService>();

// ==========================================
// 4. SERVICIOS BASE DE LA API
// ==========================================
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LicenseFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IntegraPro ERP - Sistema de Gestión General",
        Version = "v1",
        Description = "API para gestión de inventarios, facturación y licenciamiento."
    });
});

var app = builder.Build();

// ==========================================
// 5. PIPELINE DE SOLICITUDES (Middlewares)
// ==========================================

// Usar CORS antes de MapControllers
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IntegraPro API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();