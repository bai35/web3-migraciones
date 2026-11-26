using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GimnasioApi.Data;
using GimnasioApi.Middlewares;
using GimnasioApi.Repositorios;
using GimnasioApi.Seguridad;
using GimnasioApi.Servicios;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Controllers + validación centralizada ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Los errores de validación ([Required], [Range]...) salen con el mismo formato que el resto de errores.
        o.InvalidModelStateResponseFactory = contexto =>
        {
            var detalles = contexto.ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x =>
                    string.IsNullOrWhiteSpace(x.ErrorMessage) ? $"Valor inválido en '{e.Key}'." : x.ErrorMessage))
                .ToList();

            return new BadRequestObjectResult(new { status = 400, error = "Los datos enviados no son válidos.", detalles });
        };
    });

// ---------- Base de datos ----------
builder.Services.AddDbContext<GimnasioDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Gimnasio")));

// ---------- JWT ----------
builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOpciones>() ?? new JwtOpciones();
if (jwt.Clave.Length < 32)
    throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres (revisa appsettings.json).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // se usan los nombres cortos: sub, role, perfilId
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emisor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        // Respuestas JSON personalizadas para 401 (sin token / token inválido) y 403 (rol insuficiente).
        o.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                var mensaje = contexto.AuthenticateFailure is SecurityTokenExpiredException
                    ? "El token ha expirado. Usa /api/auth/refresh o inicia sesión de nuevo."
                    : "Debes autenticarte con un token JWT válido.";

                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                contexto.Response.ContentType = "application/json";
                await contexto.Response.WriteAsync(JsonSerializer.Serialize(new { status = 401, error = mensaje }));
            },
            OnForbidden = async contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                contexto.Response.ContentType = "application/json";
                await contexto.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    status = 403,
                    error = "Tu rol no tiene permiso para usar este recurso."
                }));
            }
        };
    });

builder.Services.AddAuthorization(o =>
    o.AddPolicy(AccesoSocioRequirement.Politica, p => p.Requirements.Add(new AccesoSocioRequirement())));
builder.Services.AddSingleton<IAuthorizationHandler, AccesoSocioHandler>();

// ---------- Swagger (con botón Authorize para pegar el JWT) ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Gimnasio - Socios, asistencias y planes",
        Version = "v1",
        Description = "API REST con .NET 8, Entity Framework Core, SQL Server y seguridad JWT por roles " +
                      "(ADMIN, ENTRENADOR, SOCIO)."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega solo el access token (sin escribir la palabra Bearer)."
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

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

// ---------- Inyección de dependencias ----------
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
builder.Services.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
builder.Services.AddScoped<IReportesRepositorio, ReportesRepositorio>();
builder.Services.AddSingleton<IGeneradorJwt, GeneradorJwt>();

builder.Services.AddScoped<IAccesoSocioServicio, AccesoSocioServicio>();
builder.Services.AddScoped<IAuthServicio, AuthServicio>();
builder.Services.AddScoped<IEntrenadorServicio, EntrenadorServicio>();
builder.Services.AddScoped<IMembresiaServicio, MembresiaServicio>();
builder.Services.AddScoped<ISocioServicio, SocioServicio>();
builder.Services.AddScoped<IAsistenciaServicio, AsistenciaServicio>();
builder.Services.AddScoped<IPlanServicio, PlanServicio>();
builder.Services.AddScoped<IEjercicioServicio, EjercicioServicio>();

var app = builder.Build();

// Prepara la base de datos (ver Data/InicializadorBD.cs) y crea el ADMIN inicial si no existe.
await InicializadorBD.EjecutarAsync(app.Services, app.Configuration);

app.UseMiddleware<ManejoErroresMiddleware>();

app.UseStaticFiles(); // sirve wwwroot/swagger-custom.css

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gimnasio API v1");
    c.RoutePrefix = string.Empty;                 // Swagger se abre en la raíz
    c.DocumentTitle = "Gimnasio - API";
    c.InjectStylesheet("/swagger-custom.css");
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.DefaultModelsExpandDepth(-1);
    c.EnableFilter();
    c.DisplayRequestDuration();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
