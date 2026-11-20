using IntegracionSunatBeta.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntegracionSunatBeta.Api.Infrastructure.Persistence.Configurations
{
    public class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
    {
        public void Configure(EntityTypeBuilder<Comprobante> builder)
        {
            builder.ToTable("Comprobantes");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Tipo).HasConversion<int>().IsRequired();
            builder.Property(x => x.Serie).HasMaxLength(4).IsRequired();
            builder.Property(x => x.Correlativo).IsRequired();
            builder.HasIndex(x => new { x.Serie, x.Correlativo }).IsUnique();

            builder.Property(x => x.FechaEmision).HasColumnType("date").IsRequired();
            builder.Property(x => x.Estado).HasConversion<int>().IsRequired();
            builder.HasIndex(x => x.Estado);

            builder.Property(x => x.CodigoCdr).HasMaxLength(50);
            builder.Property(x => x.MensajeSunat).HasMaxLength(500);
            builder.Property(x => x.Intentos).IsRequired();
            builder.Property(x => x.ActualizadoUtc).IsRequired();
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Ignore(x => x.NumeroCompleto);

            builder.HasMany(x => x.HistorialIntentos)
                .WithOne(i => i.Comprobante)
                .HasForeignKey(i => i.ComprobanteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
