using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Services;
using IntegraPro.DataAccess.Factory;
using Microsoft.OpenApi.Models;
using IntegraPro.API.Filters; // Asegúrate de que esta referencia coincida con tu carpeta de filtros

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE CONEXIÓN
// ==========================================
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-AR7JSQE\\SQLEXPRESS;Database=ERP_SistemaPro;Integrated Security=SSPI;TrustServerCertificate=True;";

// ==========================================
// 2. INYECCIÓN DE DEPENDENCIAS (N-Capas)
// ==========================================

// --- Capa de Datos (Factories) ---
builder.Services.AddScoped(sp => new UsuarioFactory(connectionString));
builder.Services.AddScoped(sp => new ProductoFactory(connectionString));
builder.Services.AddScoped(sp => new CategoriaFactory(connectionString));
builder.Services.AddScoped(sp => new ConfiguracionFactory(connectionString));

// --- Capa de Lógica (Services) ---
builder.Services.AddScoped<LicenciaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
// LÍNEA AÑADIDA: Registro de Categoría para solucionar el error 500
builder.Services.AddScoped<ICategoriaService, CategoriaService>();

// ==========================================
// 3. SERVICIOS BASE DE LA API
// ==========================================
// Se activa la validación de licencia y sesión automática en TODO el sistema
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
        Description = "API para gestión de inventarios, facturación y licenciamiento con control de hardware."
    });
});

var app = builder.Build();

// ==========================================
// 4. PIPELINE DE SOLICITUDES (Middlewares)
// ==========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IntegraPro API v1");
    });
}

app.UseHttpsRedirection();

// Importante: El orden de estos middlewares es vital
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();