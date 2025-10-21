using GestaoCondominio.ControlePortaria.Api.Repositories;
using GestaoCondominio.ControlePortaria.Api.Services;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using GestaoCondominio.ControlePortaria.Api.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<IUnidadeService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<UnidadeService>>();
    var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "condominios.json");
    return new UnidadeService(jsonPath, logger);
});

builder.Services.AddSingleton<IAutorizacaoRepository, AutorizacaoRepositoryJson>();
builder.Services.AddScoped<IAutorizacaoService, AutorizacaoService>();

// MVC + Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        //o.JsonSerializerOptions.PropertyNamingPolicy = null; // manter o casing dos DTOs V2
    });

//var corsSpecificOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();

//// CORS - DEV(Permissivo - sem credenciais)
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("CorsDev", policy =>
//    {
//        policy
//            .WithOrigins(corsSpecificOrigins ?? []);
//    });
//});

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

app.UseHttpsRedirection();

app.UseAuthorization();
//app.UseCors("CorsDev");

app.MapControllers();

app.Run();
