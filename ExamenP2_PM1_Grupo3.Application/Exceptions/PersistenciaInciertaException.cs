namespace ExamenP2_PM1_Grupo3.Application.Exceptions;

public sealed class PersistenciaInciertaException(Exception innerException)
    : Exception("No se pudo confirmar el guardado del contacto en SQL Server.", innerException);
