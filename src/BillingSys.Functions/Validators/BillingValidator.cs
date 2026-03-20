using BillingSys.Functions.Functions;
using BillingSys.Shared.DTOs;
using FluentValidation;

namespace BillingSys.Functions.Validators;

public class ProcessWeeklyBillingValidator : AbstractValidator<ProcessWeeklyBillingRequest>
{
    public ProcessWeeklyBillingValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.WeekNumber).InclusiveBetween(1, 53);
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.SelectedCustomerIds).NotEmpty()
            .WithMessage("At least one customer must be selected");
    }
}

public class ProjectBillingRequestValidator : AbstractValidator<ProjectBillingRequest>
{
    public ProjectBillingRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Projects).NotEmpty()
            .WithMessage("At least one project must be included");
        RuleForEach(x => x.Projects).ChildRules(line =>
        {
            line.RuleFor(l => l.ProjectCode).NotEmpty();
            line.RuleFor(l => l.HoursToBill).GreaterThan(0);
        });
    }
}

public class ProcessEdiBillingValidator : AbstractValidator<ProcessEdiBillingRequest>
{
    public ProcessEdiBillingValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.SelectedSiteIds).NotEmpty()
            .WithMessage("At least one site must be selected");
    }
}
