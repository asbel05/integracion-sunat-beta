using FluentValidation;
using IntegracionSunatBeta.Api.Application.DTOs;
using IntegracionSunatBeta.Api.Domain.Enums;

namespace IntegracionSunatBeta.Api.Application.Validators
{
    public class CrearPedidoRequestValidator : AbstractValidator<CrearPedidoRequest>
    {
        public CrearPedidoRequestValidator()
        {
            RuleFor(x => x.ClienteTipoDoc)
                .NotEmpty().WithMessage("El tipo de documento es obligatorio.")
                .Must(t => t == "1" || t == "6")
                .WithMessage("El tipo de documento debe ser '1' (DNI) o '6' (RUC).");
            RuleFor(x => x.ClienteNumDoc)
                .NotEmpty().WithMessage("El número de documento es obligatorio.");
            When(x => x.ClienteTipoDoc == "1", () =>
            {
                RuleFor(x => x.ClienteNumDoc)
                    .Matches(@"^\d{8}$")
                    .WithMessage("El DNI debe tener exactamente 8 dígitos numéricos.");
            });
            When(x => x.ClienteTipoDoc == "6", () =>
            {
                RuleFor(x => x.ClienteNumDoc)
                    .Matches(@"^(10|15|17|20)\d{9}$")
                    .WithMessage("El RUC debe tener 11 dígitos válidos (iniciar en 10, 15, 17 o 20).");
            });
            RuleFor(x => x.TipoComprobante)
                .IsInEnum().WithMessage("Tipo de comprobante inválido.");
            When(x => x.TipoComprobante == TipoComprobante.Factura, () =>
            {
                RuleFor(x => x.ClienteTipoDoc)
                    .Equal("6")
                    .WithMessage("Para emitir una Factura, el cliente debe tener RUC (tipo '6').");
            });
            RuleFor(x => x.ClienteNombre)
                .NotEmpty().WithMessage("El nombre o razón social del cliente es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no puede exceder los 150 caracteres.");
            RuleFor(x => x.Detalles)
                .NotEmpty().WithMessage("El pedido debe tener al menos un ítem.")
                .Must(d => d == null || d.Count <= 100)
                .WithMessage("El pedido no puede tener más de 100 ítems.");
            RuleForEach(x => x.Detalles)
                .SetValidator(new CrearPedidoDetalleRequestValidator());
        }
    }
    public class CrearPedidoDetalleRequestValidator : AbstractValidator<CrearPedidoDetalleRequest>
    {
        public CrearPedidoDetalleRequestValidator()
        {
            RuleFor(x => x.Descripcion)
                .NotEmpty().WithMessage("La descripción del ítem es obligatoria.")
                .MaximumLength(200).WithMessage("La descripción no puede exceder los 200 caracteres.");
            RuleFor(x => x.Cantidad)
                .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.")
                .LessThanOrEqualTo(100_000).WithMessage("La cantidad no puede exceder 100,000.");
            RuleFor(x => x.PrecioUnitario)
                .GreaterThan(0).WithMessage("El precio unitario (sin IGV) debe ser mayor a 0.")
                .LessThanOrEqualTo(1_000_000m).WithMessage("El precio unitario no puede exceder 1,000,000.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .WithMessage("El precio unitario solo puede tener hasta 2 decimales.");
        }
    }
}