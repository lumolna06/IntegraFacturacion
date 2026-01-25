using IntegraPro.AppLogic.Interfaces;
using IntegraPro.AppLogic.Services;
using IntegraPro.DataAccess.Factory;
using Microsoft.OpenApi.Models;
using IntegraPro.API.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE CONEXIÓN
// ==========================================
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-AR7JSQE\\SQLEXPRESS;Database=ERP_SistemaPro;Integrated Security=SSPI;TrustServerCertificate=True;";

// ==========================================
// 2. CONFIGURACIÓN DE CORS Y AUTENTICACIÓN
// ==========================================
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// --- Configuración de Autenticación JWT ---
// Sincronizamos la clave, emisor y audiencia con el UsuarioService usando appsettings.json
var jwtKey = builder.Configuration["Jwt:Key"] ?? "EstaEsUnaClaveSecretaMuyLargaDeAlMenos32Caracteres2026!";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),

        // Validación dinámica: Debe coincidir EXACTAMENTE con lo que genera el UsuarioService
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "IntegraProAPI",

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "IntegraProUsers",

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
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
builder.Services.AddScoped(sp => new ProveedorFactory(connectionString));
builder.Services.AddScoped(sp => new CompraFactory(connectionString));
builder.Services.AddScoped(sp => new AbonoFactory(connectionString));
builder.Services.AddScoped(sp => new ProformaFactory(connectionString));

// --- Capa de Lógica (Services) ---
builder.Services.AddScoped<LicenciaService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICompraService, CompraService>();
builder.Services.AddScoped<IProformaService, ProformaService>();
builder.Services.AddScoped<IAbonoService, AbonoService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped(sp => new XmlParserService(connectionString));

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
        Description = "API para gestión de inventarios, facturación, compras y licenciamiento."
    });

    // Configuración para que Swagger permita enviar el Token JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. \r\n\r\n Escriba 'Bearer' [espacio] y el token abajo.\r\n\r\nEjemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ==========================================
// 5. PIPELINE DE SOLICITUDES (Middlewares)
// ==========================================

// IMPORTANTE: El orden de los middlewares define el éxito de la petición
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

// El orden aquí es CRÍTICO: Authentication siempre debe ir antes que Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();