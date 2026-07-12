using System;

namespace Pacaj.Portal.Models
{
    public enum ProjectPhase
    {
        Discovery = 0,
        Development = 1,
        Testing = 2,
        Live = 3,
        Maintenance = 4,
        Archived = 5
    }

    public class ClientProject
    {
        public int Id { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string LiveUrl { get; set; } = string.Empty;
        public ProjectPhase Phase { get; set; } = ProjectPhase.Live;
        public bool LastHealthCheckOnline { get; set; } = true;
        public int LastResponseTimeMs { get; set; }
        public DateTime? SslExpiresAt { get; set; }
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class SiteHealthCheckResult
    {
        public int ProjectId { get; set; }
        public string Url { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public bool SslValid { get; set; }
        public int SslDaysRemaining { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MonitoringSummary
    {
        public int TotalMonitoredSites { get; set; }
        public int SitesOnline { get; set; }
        public int SitesDegraded { get; set; }
        public double AverageLatencyMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
