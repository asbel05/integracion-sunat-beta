namespace IntegracionSunatBeta.Api.Domain.Entities
{
    public class ComprobanteIntento
    {
        public long Id { get; set; }
        public Guid ComprobanteId { get; set; }
        public int NumeroIntento { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public string? DetalleError { get; set; }
        public DateTime FechaUtc { get; set; }
        public Comprobante? Comprobante { get; set; }
    }
}
