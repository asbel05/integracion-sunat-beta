namespace IntegracionSunatBeta.Api.Infrastructure.Messaging
{
    public class ServiceBusOptions
    {
        public const string SectionName = "ServiceBus";
        public string ConnectionString { get; set; } = string.Empty;
        public string QueueName { get; set; } = "comprobantes-queue";
    }
}
