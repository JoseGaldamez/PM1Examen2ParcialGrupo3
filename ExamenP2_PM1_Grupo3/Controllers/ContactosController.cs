using System.Globalization;
using ExamenP2_PM1_Grupo3.Application.DTOs;
using ExamenP2_PM1_Grupo3.Application.Exceptions;
using ExamenP2_PM1_Grupo3.Application.Services;
using ExamenP2_PM1_Grupo3.Filters;
using ExamenP2_PM1_Grupo3.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExamenP2_PM1_Grupo3.Controllers;

[ApiController]
[Route("api/contactos")]
public sealed class ContactosController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ContactoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContactoResponse>>> Listar(
        [FromQuery] string? buscar, [FromServices] ConsultarContactos contactos,
        CancellationToken cancellationToken)
    {
        return Ok(await contactos.ListarAsync(buscar, cancellationToken));
    }

    [HttpGet("{id:int}/archivo")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerArchivo(int id, [FromServices] ConsultarContactos contactos,
        CancellationToken cancellationToken)
    {
        var url = await contactos.ObtenerArchivoAsync(id, cancellationToken);
        return url is null ? NotFound() : Redirect(url);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ValidarVideoAttribute.MaximoSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ValidarVideoAttribute.MaximoSolicitudBytes)]
    [ValidarVideo]
    [ProducesResponseType<ContactoResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ContactoResponse>> Crear(
        [FromForm] CrearContactoForm form, [FromServices] ContactoService contactos,
        CancellationToken cancellationToken)
    {
        var error = ValidarCoordenadas(form, out var latitud, out var longitud);
        if (error is not null)
        {
            return error;
        }

        await using var contenido = form.Archivo.OpenReadStream();
        try
        {
            var contacto = await contactos.CrearAsync(form.Nombre, form.Telefono, latitud, longitud,
                contenido, Path.GetFileName(form.Archivo.FileName), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, contacto);
        }
        catch (VideoInvalidoException excepcion)
        {
            return ValidarVideoAttribute.Error(StatusCodes.Status415UnsupportedMediaType, excepcion.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ValidarVideoAttribute.MaximoSolicitudBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = ValidarVideoAttribute.MaximoSolicitudBytes)]
    [ValidarVideo]
    [ProducesResponseType<ContactoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactoResponse>> Actualizar(int id,
        [FromForm] ActualizarContactoForm form, [FromServices] ContactoService contactos,
        CancellationToken cancellationToken)
    {
        var error = ValidarCoordenadas(form, out var latitud, out var longitud);
        if (error is not null)
        {
            return error;
        }

        await using var contenido = form.Archivo?.OpenReadStream();
        try
        {
            var contacto = await contactos.ActualizarAsync(id, form.Nombre, form.Telefono,
                latitud, longitud, contenido, Path.GetFileName(form.Archivo?.FileName), cancellationToken);
            return contacto is null ? NotFound() : Ok(contacto);
        }
        catch (VideoInvalidoException excepcion)
        {
            return ValidarVideoAttribute.Error(StatusCodes.Status415UnsupportedMediaType, excepcion.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ContactoResponse>> ActualizarDesdeJson(int id,
        [FromBody] ActualizarContactoJson datos, [FromServices] ContactoService contactos,
        CancellationToken cancellationToken)
    {
        var contacto = await contactos.ActualizarAsync(id, datos.Nombre, datos.Telefono,
            datos.Latitud, datos.Longitud, null, null, cancellationToken);
        return contacto is null ? NotFound() : Ok(contacto);
    }

    [HttpPatch("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType<ContactoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactoResponse>> Editar(int id,
        [FromBody] EditarContactoJson datos, [FromServices] ContactoService contactos,
        CancellationToken cancellationToken)
    {
        var contacto = await contactos.ActualizarAsync(id, datos.Nombre, datos.Telefono,
            datos.Latitud, datos.Longitud, null, null, cancellationToken);
        return contacto is null ? NotFound() : Ok(contacto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id, [FromServices] ContactoService contactos,
        CancellationToken cancellationToken)
    {
        return await contactos.EliminarAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private ActionResult? ValidarCoordenadas(ContactoForm form, out decimal latitud, out decimal longitud)
    {
        var cultura = CultureInfo.InvariantCulture;
        if (!decimal.TryParse(form.Latitud, NumberStyles.Float, cultura, out latitud) ||
            latitud is < -90 or > 90)
        {
            ModelState.AddModelError(nameof(form.Latitud), "La latitud debe estar entre -90 y 90.");
        }

        if (!decimal.TryParse(form.Longitud, NumberStyles.Float, cultura, out longitud) ||
            longitud is < -180 or > 180)
        {
            ModelState.AddModelError(nameof(form.Longitud), "La longitud debe estar entre -180 y 180.");
        }

        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }
}
