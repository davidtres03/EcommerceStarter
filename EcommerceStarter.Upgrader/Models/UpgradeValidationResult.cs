namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Result of upgrade validation check
/// </summary>
public class UpgradeValidationResult
{
    /// <summary>
    /// Whether the upgrade can proceed
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Error message if upgrade cannot proceed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// General informational message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Whether there are warnings (but upgrade can still proceed)
    /// </summary>
    public bool HasWarnings { get; set; }

    /// <summary>
    /// Warning message if any
    /// </summary>
    public string? WarningMessage { get; set; }

    /// <summary>
    /// Any breaking changes in the target version
    /// </summary>
    public string[]? BreakingChanges { get; set; }
}
