namespace ExamenP2_PM1_Grupo3.Domain.Entities;

public sealed record Contacto(
    string Nombre,
    string Telefono,
    decimal Latitud,
    decimal Longitud,
    string VideoUrl)
{
    public const int MaximoNombre = 150;
    public const int MaximoTelefono = 30;
    public const int MaximoVideoUrl = 2048;
    public const byte DecimalesCoordenadas = 7;
}
