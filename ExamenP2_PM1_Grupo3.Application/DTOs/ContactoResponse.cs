namespace ExamenP2_PM1_Grupo3.Application.DTOs;

public sealed record ContactoResponse(
    int Id, string Nombre, string Telefono, decimal Latitud, decimal Longitud, string Archivo);
