using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserFeed.Application.UseCases;
using UserFeed.Domain.Interfaces;
using UserFeed.Infrastructure.Adapters.External;
using UserFeed.Infrastructure.Adapters.Messaging;
using UserFeed.Infrastructure.Adapters.Persistence;
using UserFeed.Infrastructure.Configuration;
using UserFeed.Infrastructure.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// Configuración
var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>() ?? new MongoDbSettings();
var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
var catalogSettings = builder.Configuration.GetSection("CatalogService").Get<CatalogServiceSettings>() ?? new CatalogServiceSettings();
var orderSettings = builder.Configuration.GetSection("OrderService").Get<OrderServiceSettings>() ?? new OrderServiceSettings();

// Registrar configuraciones
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton(catalogSettings);
builder.Services.AddSingleton(orderSettings);

// Registrar repositorios (Adapters - Infrastructure)
builder.Services.AddSingleton<IUserCommentRepository>(sp => new MongoUserCommentRepository(mongoSettings));
builder.Services.AddSingleton<IEventPublisher>(sp => new RabbitMqEventPublisher(rabbitSettings));

// Registrar conexión a RabbitMQ para servicios externos
var rabbitFactory = new RabbitMQ.Client.ConnectionFactory()
{
    HostName = rabbitSettings.HostName,
    Port = rabbitSettings.Port,
    UserName = rabbitSettings.UserName,
    Password = rabbitSettings.Password
};
builder.Services.AddSingleton<RabbitMQ.Client.IConnection>(rabbitFactory.CreateConnection());

// Registrar servicios externos
// ICatalogService: Usa RabbitMQ con patrón Request-Reply asíncrono
builder.Services.AddScoped<ICatalogService>(sp => 
{
    var connection = sp.GetRequiredService<RabbitMQ.Client.IConnection>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger<RabbitCatalogAdapter>();
    return new RabbitCatalogAdapter(connection, logger);
});

// IOrderService: Sigue usando HTTP (puede migrarse a RabbitMQ después)
builder.Services.AddHttpClient<IOrderService, OrderServiceAdapter>((sp, client) =>
{
    var settings = sp.GetRequiredService<OrderServiceSettings>();
    client.BaseAddress = new Uri(settings.BaseUrl);
});

// HttpClient genérico para cualquier servicio
builder.Services.AddHttpClient();

// Registrar casos de uso (Application)
builder.Services.AddScoped<CreateCommentUseCase>();
builder.Services.AddScoped<GetCommentsByArticleUseCase>();
builder.Services.AddScoped<UpdateCommentUseCase>();
builder.Services.AddScoped<DeleteCommentUseCase>();
builder.Services.AddScoped<GetCommentsByUserUseCase>();

// Registrar Background Services para escuchar eventos de RabbitMQ
// El listener consulta HTTP real al Catalog Service y responde por RabbitMQ
builder.Services.AddHostedService<CatalogArticleExistListener>();

// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Configurar Swagger con autenticación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserFeed API",
        Version = "v1",
        Description = "Microservicio de comentarios de usuarios sobre artículos del catálogo"
    });

    // Configurar autenticación Bearer en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserFeed API V1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 UserFeed Service iniciado en puerto 5005");
Console.WriteLine("📖 Swagger disponible en http://localhost:5005");

app.Run();

// Clase parcial necesaria para WebApplicationFactory en tests
public partial class Program { }
