namespace IntegracionSunatBeta.Api.Application.Interfaces
{
    public record ComprobanteColaMensaje(Guid ComprobanteId);

    public interface IComprobanteQueuePublisher
    {
        Task PublicarAsync(IReadOnlyCollection<Guid> comprobanteIds, CancellationToken ct = default);
    }
}
