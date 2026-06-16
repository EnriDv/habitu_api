using FluentValidation;
using Habitu.Application.Features.Habits.Commands;

namespace Habitu.Application.Validators;

public class CreateHabitCommandValidator : AbstractValidator<CreateHabitCommand>
{
    public CreateHabitCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título del hábito es requerido.")
            .MaximumLength(100).WithMessage("El título no puede exceder los 100 caracteres.");

        RuleFor(x => x.FrequencyType)
            .NotEmpty().WithMessage("El tipo de frecuencia es requerido.")
            .Must(x => x == "daily" || x == "weekly_days" || x == "interval")
            .WithMessage("El tipo de frecuencia debe ser: daily, weekly_days o interval.");
    }
}