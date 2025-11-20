using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserFeed.Application.UseCases;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Adapters.External;
using UserFeed.Infrastructure.Adapters.Messaging;
using UserFeed.Infrastructure.Adapters.Persistence;
using UserFeed.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configuración
var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>() ?? new MongoDbSettings();
var rabbitSettings = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();
var authSettings = builder.Configuration.GetSection("AuthService").Get<AuthServiceSettings>() ?? new AuthServiceSettings();
var catalogSettings = builder.Configuration.GetSection("CatalogService").Get<CatalogServiceSettings>() ?? new CatalogServiceSettings();
var orderSettings = builder.Configuration.GetSection("OrderService").Get<OrderServiceSettings>() ?? new OrderServiceSettings();

// Registrar configuraciones
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton(authSettings);
builder.Services.AddSingleton(catalogSettings);
builder.Services.AddSingleton(orderSettings);

// Registrar repositorios (Adapters - Infrastructure)
builder.Services.AddSingleton<IUserCommentRepository>(sp => new MongoUserCommentRepository(mongoSettings));
builder.Services.AddSingleton<IEventPublisher>(sp => new RabbitMqEventPublisher(rabbitSettings));

// Registrar servicios externos
builder.Services.AddHttpClient<IAuthService, AuthServiceAdapter>((sp, client) =>
{
    var settings = sp.GetRequiredService<AuthServiceSettings>();
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHttpClient<ICatalogService, CatalogServiceAdapter>((sp, client) =>
{
    var settings = sp.GetRequiredService<CatalogServiceSettings>();
    client.BaseAddress = new Uri(settings.BaseUrl);
});

builder.Services.AddHttpClient<IOrderService, OrderServiceAdapter>((sp, client) =>
{
    var settings = sp.GetRequiredService<OrderServiceSettings>();
    client.BaseAddress = new Uri(settings.BaseUrl);
});

// Registrar casos de uso (Application)
builder.Services.AddScoped<CreateCommentUseCase>();
builder.Services.AddScoped<GetCommentsByArticleUseCase>();
builder.Services.AddScoped<UpdateCommentUseCase>();
builder.Services.AddScoped<DeleteCommentUseCase>();
builder.Services.AddScoped<GetArticlesWithCommentsUseCase>(); // requires ICatalogService

// Registrar consumer de RabbitMQ
builder.Services.AddHostedService(sp =>
{
    var authService = sp.GetRequiredService<IAuthService>();
    return new LogoutConsumer(rabbitSettings, authService);
});

// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false, // Los tokens nunca expiran según la arquitectura
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, parameters) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token)
        };
        
        // Validar token contra el servicio Auth
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                try
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        var token = authHeader.Substring("Bearer ".Length).Trim();
                        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                        var user = await authService.GetCurrentUserAsync(token);
                        
                        if (user == null)
                        {
                            context.Fail("Token inválido");
                        }
                    }
                }
                catch
                {
                    context.Fail("Error validando token");
                }
            }
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
