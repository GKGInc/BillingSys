using System.Net;
using BillingSys.Functions.Repositories;
using BillingSys.Shared.Enums;
using BillingSys.Shared.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BillingSys.Functions.Services;

public class AuthorizationService
{
    private readonly AuthenticationService _authService;
    private readonly IEmployeeRepository _employees;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        AuthenticationService authService,
        IEmployeeRepository employees,
        ILogger<AuthorizationService> logger)
    {
        _authService = authService;
        _employees = employees;
        _logger = logger;
    }

    #region Public Methods

    public async Task<AuthorizationResult> AuthorizeAsync(HttpRequestData req, params UserRole[] allowedRoles)
    {
        var principal = await _authService.ValidateTokenAsync(req);
        if (principal == null)
        {
            return AuthorizationResult.Unauthorized();
        }

        var email = AuthenticationService.GetUserEmail(principal);
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Authenticated user has no email claim");
            return AuthorizationResult.Unauthorized();
        }

        var employeeResult = await _employees.GetByEmailAsync(email);
        if (!employeeResult.Success || employeeResult.Data == null)
        {
            _logger.LogWarning("No employee record found for email {Email}", email);
            return AuthorizationResult.Forbidden();
        }

        if (allowedRoles.Length > 0 && !allowedRoles.Contains(employeeResult.Data.Role))
        {
            _logger.LogWarning("User {Email} with role {Role} denied access requiring {AllowedRoles}",
                email, employeeResult.Data.Role, string.Join(", ", allowedRoles));
            return AuthorizationResult.Forbidden();
        }

        return AuthorizationResult.Success(employeeResult.Data);
    }

    #endregion
}

#region Classes

public class AuthorizationResult
{
    public bool IsAuthorized { get; private set; }
    public bool IsForbidden { get; private set; }
    public Employee? Employee { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static AuthorizationResult Success(Employee employee) =>
        new() { IsAuthorized = true, Employee = employee };

    public static AuthorizationResult Unauthorized() =>
        new() { IsAuthorized = false, ErrorMessage = "Unauthorized - please sign in with your tech85.com Google account" };

    public static AuthorizationResult Forbidden() =>
        new() { IsAuthorized = false, IsForbidden = true, ErrorMessage = "You do not have permission to perform this action" };

    public async Task<HttpResponseData> ToResponseAsync(HttpRequestData req)
    {
        var statusCode = IsForbidden ? HttpStatusCode.Forbidden : HttpStatusCode.Unauthorized;
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = ErrorMessage });
        return response;
    }
}

#endregion
