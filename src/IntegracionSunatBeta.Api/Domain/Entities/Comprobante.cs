using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Domain.Entities
{
    public class Comprobante
    {
        public Guid Id { get; set; }
        public Guid PedidoId { get; set; }
        public TipoComprobante Tipo { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Correlativo { get; set; }
        public DateOnly FechaEmision { get; set; }
        public EstadoComprobante Estado { get; set; }
        public string? CodigoCdr { get; set; }
        public string? MensajeSunat { get; set; }
        public int Intentos { get; set; }
        public DateTime ActualizadoUtc { get; set; }
        public byte[] RowVersion { get; set; } = [];
        public Pedido? Pedido { get; set; }
        public List<ComprobanteIntento> HistorialIntentos { get; set; } = [];
        public string NumeroCompleto => $"{Serie}-{Correlativo:D8}";
    }
}
