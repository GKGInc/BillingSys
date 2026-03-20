using BillingSys.Shared.Enums;

namespace BillingSys.Shared.Models;

public class Employee : AuditableEntity
{
    #region Identity

    public string Id { get; set; } = string.Empty;

    #endregion

    #region Details

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }

    #endregion

    #region Access Control

    public UserRole Role { get; set; } = UserRole.User;

    #endregion

    #region Status

    public bool IsActive { get; set; } = true;

    #endregion
}
