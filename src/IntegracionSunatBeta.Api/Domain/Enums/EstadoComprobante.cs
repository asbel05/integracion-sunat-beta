namespace IntegracionSunatBeta.Api.Domain.Enums
{
    public enum EstadoComprobante
    {
        SinEnviar = 1,
        EnCola = 2,
        Procesando = 3,
        Aceptado = 4,
        Rechazado = 5,
        ErrorEnvio = 6
    }
}
