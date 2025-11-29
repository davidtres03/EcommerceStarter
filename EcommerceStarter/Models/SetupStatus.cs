using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Tracks the setup completion status for first-run experience
    /// </summary>
    public class SetupStatus
    {
        public int Id { get; set; }

        /// <summary>
        /// Is the initial setup wizard completed?
        /// </summary>
        public bool IsSetupComplete { get; set; }

        /// <summary>
        /// When was setup completed?
        /// </summary>
        public DateTime? SetupCompletedDate { get; set; }

        /// <summary>
        /// Who completed the setup?
        /// </summary>
        [StringLength(200)]
        public string? SetupCompletedBy { get; set; }

        /// <summary>
        /// Has the admin configured site branding?
        /// </summary>
        public bool HasConfiguredBranding { get; set; }

        /// <summary>
        /// Has the admin configured Stripe keys?
        /// </summary>
        public bool HasConfiguredStripe { get; set; }

        /// <summary>
        /// Has the admin added products?
        /// </summary>
        public bool HasAddedProducts { get; set; }

        /// <summary>
        /// Has the admin configured security settings?
        /// </summary>
        public bool HasConfiguredSecurity { get; set; }

        /// <summary>
        /// Platform version installed
        /// </summary>
        [StringLength(50)]
        public string? PlatformVersion { get; set; }

        /// <summary>
        /// Theme selected during setup
        /// </summary>
        [StringLength(50)]
        public string? InitialTheme { get; set; }

        /// <summary>
        /// Notes or custom data from setup
        /// </summary>
        public string? SetupNotes { get; set; }

        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}
