using BillingSys.Shared.Models;
using FluentValidation;

namespace BillingSys.Functions.Validators;

public class EmployeeValidator : AbstractValidator<Employee>
{
    public EmployeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0);
    }
}

public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.State).MaximumLength(2);
        RuleFor(x => x.ZipCode).MaximumLength(10);
        RuleFor(x => x.PaymentNetDays).InclusiveBetween(0, 365);
    }
}

public class ServiceItemValidator : AbstractValidator<ServiceItem>
{
    public ServiceItemValidator()
    {
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
    }
}
