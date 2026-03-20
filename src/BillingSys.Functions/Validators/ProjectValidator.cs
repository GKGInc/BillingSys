using BillingSys.Shared.DTOs;
using FluentValidation;

namespace BillingSys.Functions.Validators;

public class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.ProjectCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServiceItemCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuotedHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CustomerPO).MaximumLength(50);
        RuleFor(x => x.ProgrammerId).MaximumLength(10);
    }
}

public class UpdateProjectValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServiceItemCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuotedHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AdditionalHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CustomerPO).MaximumLength(50);
        RuleFor(x => x.ProgrammerId).MaximumLength(10);
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<BillingSys.Shared.Models.ProjectStatus>(s, true, out _))
            .WithMessage("Invalid project status");
    }
}
