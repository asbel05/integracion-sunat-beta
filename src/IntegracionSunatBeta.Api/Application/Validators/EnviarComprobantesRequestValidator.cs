using FluentValidation;
using IntegracionSunatBeta.Api.Application.DTOs;

namespace IntegracionSunatBeta.Api.Application.Validators
{
    public class EnviarComprobantesRequestValidator : AbstractValidator<EnviarComprobantesRequest>
    {
        public EnviarComprobantesRequestValidator()
        {
            RuleFor(x => x.ComprobanteIds)
                .NotEmpty().WithMessage("Debes seleccionar al menos un comprobante para enviar.")
                .Must(ids => ids == null || ids.Count <= 50)
                .WithMessage("No se pueden enviar más de 50 comprobantes por lote.")
                .Must(ids => ids == null || ids.All(id => id != Guid.Empty))
                .WithMessage("La lista contiene uno o más IDs vacíos inválidos.")
                .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
                .WithMessage("La lista contiene IDs de comprobantes duplicados.");
        }
    }
}
