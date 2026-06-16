using FluentValidation;
using Habitu.Application.Features.Auth.Commands;

namespace Habitu.Application.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

        RuleFor(x => x.Request.FullName)
            .NotEmpty().WithMessage("El nombre completo es requerido.");

        RuleFor(x => x.Request.UniversityHeadquarters)
            .NotEmpty().WithMessage("La sede de la universidad es requerida.");
    }
}