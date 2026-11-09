using Catalogo.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microservicios.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CatalogoDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CatalogoConnection")
        ?? throw new InvalidOperationException("Falta la cadena de conexión CatalogoConnection.")));

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
