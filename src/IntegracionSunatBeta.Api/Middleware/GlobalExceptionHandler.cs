using FluentValidation;
using IntegracionSunatBeta.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegracionSunatBeta.Api.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            ProblemDetails problemDetails;
            switch (exception)
            {
                case ValidationException validationEx:
                    var errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
                    problemDetails = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Error de validación",
                        Detail = "Uno o más campos de la solicitud no son válidos.",
                        Instance = httpContext.Request.Path
                    };
                    break;
                case DomainException domainEx:
                    var (statusCode, title) = domainEx.Tipo switch
                    {
                        TipoErrorDominio.Validacion => (StatusCodes.Status400BadRequest, "Error de validación de negocio"),
                        TipoErrorDominio.NoEncontrado => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                        TipoErrorDominio.ConflictoEstado => (StatusCodes.Status409Conflict, "Conflicto de estado"),
                        TipoErrorDominio.ServicioNoDisponible => (StatusCodes.Status503ServiceUnavailable, "Servicio temporalmente no disponible"),
                        _ => (StatusCodes.Status400BadRequest, "Error de dominio")
                    };
                    problemDetails = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = domainEx.Message,
                        Instance = httpContext.Request.Path
                    };
                    break;
                case DbUpdateConcurrencyException:
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflicto de concurrencia",
                        Detail = "El registro fue modificado por otro proceso. Intenta nuevamente.",
                        Instance = httpContext.Request.Path
                    };
                    break;
                default:
                    _logger.LogError(exception, "Error inesperado procesando {Method} {Path}",
                        httpContext.Request.Method, httpContext.Request.Path);
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Error interno del servidor",
                        Detail = "Ocurrió un error inesperado al procesar la solicitud.",
                        Instance = httpContext.Request.Path
                    };
                    break;
            }
            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = problemDetails
            });
        }
    }
}
