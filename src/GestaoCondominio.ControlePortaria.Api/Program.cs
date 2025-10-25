using GestaoCondominio.ControlePortaria.Api.Repositories;
using GestaoCondominio.ControlePortaria.Api.Services;
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

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
