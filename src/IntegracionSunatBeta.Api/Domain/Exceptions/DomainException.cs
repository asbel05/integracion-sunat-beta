namespace IntegracionSunatBeta.Api.Domain.Exceptions;

public enum TipoErrorDominio
{
    Validacion = 1,
    NoEncontrado = 2,
    ConflictoEstado = 3
}

public class DomainException : Exception
{
    public TipoErrorDominio Tipo { get; }

    public DomainException(string message, TipoErrorDominio tipo = TipoErrorDominio.ConflictoEstado) : base(message)
    {
        Tipo = tipo;
    }
}
