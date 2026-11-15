using ExamenP2_PM1_Grupo3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace ExamenP2_PM1_Grupo3.Filters;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidarVideoAttribute : Attribute, IAsyncResourceFilter
{
    private const long MaximoVideoBytes = 20_000_000;
    // El servidor deja margen para leer el multipart y devolver el error JSON del archivo.
    internal const long MaximoSolicitudBytes = 30_000_000;
    private const string MensajeTamano = "El archivo debe pesar como máximo 20 MB.";
    private const string MensajeFormato = "El archivo debe ser un video MP4, 3GP, WebM, MOV, MKV o MPEG-TS válido.";

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var cancellationToken = context.HttpContext.RequestAborted;
        if (!request.HasFormContentType)
        {
            context.Result = Error(StatusCodes.Status415UnsupportedMediaType,
                "Envía el video en un formulario multipart/form-data.");
            return;
        }

        if (request.ContentLength > MaximoSolicitudBytes)
        {
            context.Result = Error(StatusCodes.Status400BadRequest, MensajeTamano);
            return;
        }

        IFormCollection formulario;
        try
        {
            formulario = await request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException error) when (error.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.Result = Error(StatusCodes.Status400BadRequest, MensajeTamano);
            return;
        }
        catch (InvalidDataException)
        {
            context.Result = Error(StatusCodes.Status400BadRequest, "No se pudo leer el formulario del archivo.");
            return;
        }

        var archivo = formulario.Files.GetFile(ContactoForm.CampoArchivo);
        if (archivo is null)
        {
            await next();
            return;
        }

        if (archivo.Length == 0 || archivo.Length > MaximoVideoBytes)
        {
            var mensaje = archivo.Length == 0 ? "El archivo está vacío." : MensajeTamano;
            context.Result = Error(StatusCodes.Status400BadRequest, mensaje);
            return;
        }

        if (!await FormatoPermitidoAsync(archivo, cancellationToken))
        {
            context.Result = Error(StatusCodes.Status415UnsupportedMediaType, MensajeFormato);
            return;
        }

        await next();
    }

    internal static JsonResult Error(int statusCode, string mensaje)
    {
        return new JsonResult(new { mensaje }) { StatusCode = statusCode };
    }

    private static async Task<bool> FormatoPermitidoAsync(IFormFile archivo, CancellationToken cancellationToken)
    {
        if (!MediaTypeHeaderValue.TryParse(archivo.ContentType, out var contentType))
        {
            return false;
        }

        var encabezado = new byte[(int)Math.Min(archivo.Length, 512)];
        await using var contenido = archivo.OpenReadStream();
        await contenido.ReadExactlyAsync(encabezado, cancellationToken);
        return CoincideFirma(contentType.MediaType.ToString().ToLowerInvariant(), encabezado);
    }

    private static bool CoincideFirma(string mime, ReadOnlySpan<byte> encabezado)
    {
        return mime switch
        {
            "video/mp4" => EsMp4(encabezado),
            "video/3gpp" => Es3Gp(encabezado),
            "video/quicktime" => EsMov(encabezado),
            "video/webm" => EsWebM(encabezado),
            "video/matroska" or "video/x-matroska" => EsMatroska(encabezado),
            "video/mp2t" => EsMpegTs(encabezado),
            "application/octet-stream" => EsMp4(encabezado) || Es3Gp(encabezado) ||
                EsWebM(encabezado) || EsMatroska(encabezado) || EsMpegTs(encabezado) ||
                MarcaPrincipal(encabezado).SequenceEqual("qt  "u8),
            _ => false
        };
    }

    private static bool EsMp4(ReadOnlySpan<byte> encabezado)
    {
        var marca = MarcaPrincipal(encabezado);
        return marca.StartsWith("iso"u8) || marca.StartsWith("mp4"u8) ||
            marca.SequenceEqual("avc1"u8) || marca.SequenceEqual("hvc1"u8) ||
            marca.SequenceEqual("M4V "u8) || marca.SequenceEqual("MSNV"u8) ||
            marca.SequenceEqual("dash"u8);
    }

    private static bool Es3Gp(ReadOnlySpan<byte> encabezado)
    {
        return MarcaPrincipal(encabezado).StartsWith("3gp"u8);
    }

    private static bool EsMov(ReadOnlySpan<byte> encabezado)
    {
        if (encabezado.Length < 12)
        {
            return false;
        }

        var tipoCaja = encabezado.Slice(4, 4);
        return MarcaPrincipal(encabezado).SequenceEqual("qt  "u8) ||
            tipoCaja.SequenceEqual("moov"u8) || tipoCaja.SequenceEqual("mdat"u8) ||
            tipoCaja.SequenceEqual("wide"u8);
    }

    private static ReadOnlySpan<byte> MarcaPrincipal(ReadOnlySpan<byte> encabezado)
    {
        return encabezado.Length >= 12 && encabezado.Slice(4, 4).SequenceEqual("ftyp"u8)
            ? encabezado.Slice(8, 4)
            : ReadOnlySpan<byte>.Empty;
    }

    private static bool EsWebM(ReadOnlySpan<byte> encabezado)
    {
        ReadOnlySpan<byte> tipoWebm = [0x42, 0x82, 0x84, .. "webm"u8];
        return EsEbml(encabezado, tipoWebm);
    }

    private static bool EsMatroska(ReadOnlySpan<byte> encabezado)
    {
        ReadOnlySpan<byte> tipoMatroska = [0x42, 0x82, 0x88, .. "matroska"u8];
        return EsEbml(encabezado, tipoMatroska);
    }

    private static bool EsEbml(ReadOnlySpan<byte> encabezado, ReadOnlySpan<byte> tipoDocumento)
    {
        ReadOnlySpan<byte> firmaEbml = [0x1A, 0x45, 0xDF, 0xA3];
        return encabezado.StartsWith(firmaEbml) && encabezado.IndexOf(tipoDocumento) >= 0;
    }

    private static bool EsMpegTs(ReadOnlySpan<byte> encabezado)
    {
        // MPEG-TS usa paquetes de 188 bytes. M2TS antepone 4 bytes a cada paquete.
        const byte sincronizacion = 0x47;
        return (encabezado.Length > 376 && encabezado[0] == sincronizacion &&
                encabezado[188] == sincronizacion && encabezado[376] == sincronizacion) ||
               (encabezado.Length > 388 && encabezado[4] == sincronizacion &&
                encabezado[196] == sincronizacion && encabezado[388] == sincronizacion);
    }
}
