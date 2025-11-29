namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Demo scenarios for installer demonstration mode
/// </summary>
public enum DemoScenario
{
    /// <summary>
    /// Show demo selection screen
    /// </summary>
    Selection,
    
    /// <summary>
    /// Demo: Fresh installation
    /// </summary>
    FreshInstall,
    
    /// <summary>
    /// Demo: Upgrade existing installation
    /// </summary>
    Upgrade,
    
    /// <summary>
    /// Demo: Reconfigure settings (reset password, etc.)
    /// </summary>
    Reconfigure,
    
    /// <summary>
    /// Demo: Repair installation
    /// </summary>
    Repair,
    
    /// <summary>
    /// Demo: Uninstall
    /// </summary>
    Uninstall
}
