using Azure.Messaging.ServiceBus;
using IntegracionSunatBeta.Api.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IntegracionSunatBeta.Api.Infrastructure.Messaging
{
    public class ServiceBusComprobantePublisher : IComprobanteQueuePublisher
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ServiceBusOptions _options;
        public ServiceBusComprobantePublisher(
            ServiceBusClient serviceBusClient,
            IOptions<ServiceBusOptions> options)
        {
            _serviceBusClient = serviceBusClient;
            _options = options.Value;
        }
        public async Task PublicarAsync(IReadOnlyCollection<Guid> comprobanteIds, CancellationToken ct = default)
        {
            await using var sender = _serviceBusClient.CreateSender(_options.QueueName);
            using var batch = await sender.CreateMessageBatchAsync(ct);
            foreach (var id in comprobanteIds)
            {
                var payload = JsonSerializer.Serialize(new ComprobanteColaMensaje(id));
                var message = new ServiceBusMessage(payload)
                {
                    ContentType = "application/json",
                    Subject = "EnviarComprobanteSunat",
                    CorrelationId = id.ToString()
                };
                if (!batch.TryAddMessage(message))
                {
                    throw new InvalidOperationException($"El mensaje del comprobante {id} excede el tamaño del lote.");
                }
            }
            if (batch.Count > 0)
            {
                await sender.SendMessagesAsync(batch, ct);
            }
        }
    }
}
