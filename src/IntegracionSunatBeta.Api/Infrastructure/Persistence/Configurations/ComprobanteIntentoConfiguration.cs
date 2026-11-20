using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegracionSunatBeta.Api.Infrastructure.Persistence.Configurations
{
    public class ComprobanteIntentoConfiguration : IEntityTypeConfiguration<ComprobanteIntento>
    {
        public void Configure(EntityTypeBuilder<ComprobanteIntento> builder)
        {
            builder.ToTable("ComprobanteIntentos");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.NumeroIntento).IsRequired();
            builder.Property(x => x.Resultado).HasMaxLength(30).IsRequired();
            builder.Property(x => x.DetalleError).HasMaxLength(500);
            builder.Property(x => x.FechaUtc).IsRequired();

            builder.HasIndex(x => new { x.ComprobanteId, x.FechaUtc });
        }
    }
}
