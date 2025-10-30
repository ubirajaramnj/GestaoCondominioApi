using GestaoCondominio.ControlePortaria.Api.DTOs;
using GestaoCondominio.ControlePortaria.Api.Infrastructure.Configuration;
using GestaoCondominio.ControlePortaria.Api.Repositories;
using GestaoCondominio.ControlePortaria.Api.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Get environment name (default to "Production" if not set)
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// Build configuration with environment-specific settings
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .Build();

builder.Configuration.AddConfiguration(config);

TimeZoneConfiguration.Inicializar();

// Add services to the container.
builder.Services.AddSingleton<IUnidadeService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<UnidadeService>>();
    var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "condominios.json");
    return new UnidadeService(jsonPath, logger);
});

builder.Services.AddSingleton<IAutorizacaoRepository, AutorizacaoRepositoryJson>();
builder.Services.AddScoped<IAutorizacaoService, AutorizacaoService>();

builder.Services.AddSingleton<IDocumentoRepository, DocumentoRepositoryJson>();
builder.Services.AddScoped<IDocumentoService, DocumentoService>();

builder.Services.AddSingleton<IComprovanteRepository, ComprovanteRepositoryJson>();
builder.Services.AddScoped<IComprovanteService, ComprovanteService>();

// DI para URLs encurtadas
builder.Services.AddSingleton<IUrlCurtaRepository, UrlCurtaRepositoryJson>();
builder.Services.AddScoped<IUrlCurtaService, UrlCurtaService>();

// ===== NOVO: Configuração de Mensageria =====
builder.Services.Configure<ConfiguracaoMensageria>(builder.Configuration.GetSection("Mensageria"));

var mensageriaConfig = builder.Configuration.GetSection("Mensageria").Get<ConfiguracaoMensageria>();

// HttpClient para chamadas à API de mensageria
builder.Services.AddHttpClient<IMensageriaService, MensageriaService>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var config = serviceProvider.GetRequiredService<IOptions<ConfiguracaoMensageria>>().Value;

        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "GestaoCondominio.ControlePortaria/1.0");
        client.DefaultRequestHeaders.Add("apikey", mensageriaConfig?.ApiKey ?? "");
    });

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.WriteIndented = false;
        //o.JsonSerializerOptions.PropertyNamingPolicy = null; // manter o casing dos DTOs V2
    });

var corsSpecificOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins(corsSpecificOrigins ?? [])
                           .AllowAnyHeader()
                           .AllowAnyMethod());
});


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// FluentValidation
//builder.Services.AddFluentValidationAutoValidation();
//builder.Services.AddValidatorsFromAssemblyContaining<CreateAutorizacaoRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.UseCors("AllowSpecificOrigin");

app.MapControllers();

app.Run();
