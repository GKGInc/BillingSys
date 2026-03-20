using BillingSys.Shared.DTOs;
using FluentValidation;

namespace BillingSys.Functions.Validators;

public class CreateTimeEntryValidator : AbstractValidator<CreateTimeEntryRequest>
{
    public CreateTimeEntryValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Date).NotEmpty().LessThanOrEqualTo(DateTime.Today.AddDays(7))
            .WithMessage("Date cannot be more than 7 days in the future");
        RuleFor(x => x.Hours).InclusiveBetween(0.25m, 24m);
        RuleFor(x => x.ProjectCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Comments).MaximumLength(500);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .When(x => x.StartTime != default && x.EndTime != default)
            .WithMessage("End time must be after start time");
    }
}

public class UpdateTimeEntryValidator : AbstractValidator<UpdateTimeEntryRequest>
{
    public UpdateTimeEntryValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Hours).InclusiveBetween(0.25m, 24m);
        RuleFor(x => x.ProjectCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Comments).MaximumLength(500);
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .When(x => x.StartTime != default && x.EndTime != default)
            .WithMessage("End time must be after start time");
    }
}
