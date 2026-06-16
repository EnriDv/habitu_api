using FluentValidation;
using Habitu.Application.Features.Auth.Commands;

namespace Habitu.Application.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("El correo electrónico es requerido.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.");
    }
}