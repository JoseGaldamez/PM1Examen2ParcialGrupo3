using ExamenP2_PM1_Grupo3.Application.Interfaces;
using ExamenP2_PM1_Grupo3.Application.Services;
using ExamenP2_PM1_Grupo3.Controllers;
using ExamenP2_PM1_Grupo3.Infrastructure.Repositories;
using ExamenP2_PM1_Grupo3.Infrastructure.Storage;
using ExamenP2_PM1_Grupo3.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var port = builder.Configuration["PORT"];
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<ContactoService>();
builder.Services.AddScoped<ConsultarContactos>();
builder.Services.AddSingleton<IContactoRepository>(_ => new SqlContactoRepository(
    RequerirVariable("ConnectionStrings:Contactos", "ConnectionStrings__Contactos")));
builder.Services.AddSingleton<IAlmacenamientoVideo>(_ => new CloudinaryVideoStorage(
    RequerirVariable("CLOUDINARY_URL", "CLOUDINARY_URL")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options => options.AddOperationTransformer(async (operation, context, cancellationToken) =>
{
    if (context.Description.ActionDescriptor is ControllerActionDescriptor action &&
        action.ControllerTypeInfo.AsType() == typeof(ContactosController) &&
        action.MethodInfo.Name == nameof(ContactosController.Actualizar) &&
        operation.RequestBody is OpenApiRequestBody { Content: { } content })
    {
        // OpenAPI representa el PUT como una sola operación con ambos tipos de contenido.
        content["application/json"] = new OpenApiMediaType
        {
            Schema = await context.GetOrCreateSchemaAsync(typeof(ActualizarContactoJson),
                cancellationToken: cancellationToken)
        };
    }
}));

var app = builder.Build();
app.UseExceptionHandler();

if (string.IsNullOrEmpty(port))
{
    app.UseHttpsRedirection();
}

app.UseSwaggerUI(options =>
{
    options.DocumentTitle = "API de contactos";
    options.SwaggerEndpoint("../openapi/v1.json", "API de contactos v1");
});

app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();

app.Run();

string RequerirVariable(string clave, string variable)
{
    var valor = builder.Configuration[clave];
    return !string.IsNullOrWhiteSpace(valor)
        ? valor
        : throw new InvalidOperationException($"Falta configurar la variable {variable}.");
}
