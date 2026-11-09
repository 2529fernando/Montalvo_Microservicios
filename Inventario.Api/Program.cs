using Inventario.Api.Data;
using Inventario.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microservicios.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<CatalogoClient>(client =>
{
    var baseUrl = builder.Configuration["Services:Catalogo:BaseUrl"]
        ?? throw new InvalidOperationException("Falta la dirección Services:Catalogo:BaseUrl.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<InventarioValidationService>();

builder.Services.AddDbContext<InventarioDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InventarioConnection")
        ?? throw new InvalidOperationException("Falta la cadena de conexión InventarioConnection.")));

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
