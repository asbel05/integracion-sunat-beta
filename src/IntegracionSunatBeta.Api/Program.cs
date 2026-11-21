using FluentValidation;
using IntegracionSunatBeta.Api.Application.Interfaces;
using IntegracionSunatBeta.Api.Application.Mappers;
using IntegracionSunatBeta.Api.Application.Services;
using IntegracionSunatBeta.Api.Application.Validators;
using IntegracionSunatBeta.Api.Infrastructure.Persistence;
using IntegracionSunatBeta.Api.Infrastructure.Repositories;
using IntegracionSunatBeta.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("No se configuró 'ConnectionStrings:DefaultConnection'");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IComprobanteRepository, ComprobanteRepository>();
builder.Services.AddSingleton<PedidoMapper>();
builder.Services.AddSingleton<ComprobanteMapper>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IComprobanteService, ComprobanteService>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearPedidoRequestValidator>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
