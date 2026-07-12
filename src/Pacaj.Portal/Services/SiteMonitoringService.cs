using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Pacaj.Portal.Models;

namespace Pacaj.Portal.Services
{
    public interface ISiteMonitoringService
    {
        Task<SiteHealthCheckResult> CheckSiteAsync(ClientProject project, CancellationToken cancellationToken = default);
        Task<MonitoringSummary> GetGlobalSummaryAsync(CancellationToken cancellationToken = default);
        IReadOnlyList<ClientProject> GetRegisteredProjects();
    }

    public class SiteMonitoringService : ISiteMonitoringService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SiteMonitoringService> _logger;
        private readonly List<ClientProject> _projects;

        public SiteMonitoringService(
            IHttpClientFactory httpClientFactory,
            ILogger<SiteMonitoringService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            // Seed production client sites managed under Pacaj digital ecosystem
            _projects = new List<ClientProject>
            {
                new() { Id = 1, ClientName = "Destina Tourism", ProjectTitle = "VIP Airport Transfer Core", LiveUrl = "https://destinatourism.com", Phase = ProjectPhase.Live },
                new() { Id = 2, ClientName = "Pacaj Digital", ProjectTitle = "Pacaj Official Portal", LiveUrl = "https://pacaj.com", Phase = ProjectPhase.Live },
                new() { Id = 3, ClientName = "Teklifver Online", ProjectTitle = "Procurement & B2B Engine", LiveUrl = "https://teklifveronline.io", Phase = ProjectPhase.Live },
                new() { Id = 4, ClientName = "AI Dubliners", ProjectTitle = "European Travel Hub", LiveUrl = "https://aidubliners.com", Phase = ProjectPhase.Maintenance }
            };
        }

        public IReadOnlyList<ClientProject> GetRegisteredProjects() => _projects.AsReadOnly();

        public async Task<SiteHealthCheckResult> CheckSiteAsync(ClientProject project, CancellationToken cancellationToken = default)
        {
            var url = NormalizeUrl(project.LiveUrl);
            var result = new SiteHealthCheckResult
            {
                ProjectId = project.Id,
                Url = project.LiveUrl,
                CheckedAt = DateTime.UtcNow
            };

            if (url == null)
            {
                result.IsOnline = false;
                result.ErrorMessage = "Malformed or missing URL protocol.";
                return result;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("SiteMonitorClient");
                var sw = Stopwatch.StartNew();
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                sw.Stop();

                result.ResponseTimeMs = sw.ElapsedMilliseconds;
                result.StatusCode = (int)response.StatusCode;
                result.IsOnline = response.IsSuccessStatusCode;

                if (!result.IsOnline)
                {
                    result.ErrorMessage = $"HTTP response indicates failure: {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.IsOnline = false;
                result.ErrorMessage = ex.Message.Length > 250 ? ex.Message[..250] : ex.Message;
                _logger.LogWarning(ex, "HTTP probe failed for {Url}", url);
            }

            // Perform deep TLS/SSL handshake analysis for HTTPS endpoints
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sslExpiry = await GetSslExpiryAsync(url, cancellationToken);
                    if (sslExpiry.HasValue)
                    {
                        project.SslExpiresAt = sslExpiry.Value;
                        result.SslValid = sslExpiry.Value > DateTime.UtcNow;
                        result.SslDaysRemaining = (int)Math.Max(0, (sslExpiry.Value - DateTime.UtcNow).TotalDays);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SSL inspection failed for {Url}", url);
                    result.SslValid = false;
                }
            }

            // Update in-memory telemetry
            project.LastHealthCheckOnline = result.IsOnline;
            project.LastResponseTimeMs = (int)result.ResponseTimeMs;
            project.LastCheckedAt = result.CheckedAt;

            return result;
        }

        public async Task<MonitoringSummary> GetGlobalSummaryAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<SiteHealthCheckResult>();
            foreach (var project in _projects)
            {
                var res = await CheckSiteAsync(project, cancellationToken);
                results.Add(res);
            }

            var onlineSites = results.Count(r => r.IsOnline);
            var degradedSites = results.Count(r => !r.IsOnline || (r.SslValid && r.SslDaysRemaining <= 14));
            var avgLatency = results.Any(r => r.IsOnline)
                ? results.Where(r => r.IsOnline).Average(r => r.ResponseTimeMs)
                : 0.0;

            return new MonitoringSummary
            {
                TotalMonitoredSites = results.Count,
                SitesOnline = onlineSites,
                SitesDegraded = degradedSites,
                AverageLatencyMs = Math.Round(avgLatency, 2),
                Timestamp = DateTime.UtcNow
            };
        }

        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return Uri.TryCreate(url, UriKind.Absolute, out _) ? url : null;
        }

        private static async Task<DateTime?> GetSslExpiryAsync(string url, CancellationToken cancellationToken)
        {
            var uri = new Uri(url);
            if (uri.Scheme != "https") return null;

            var host = uri.Host;
            var port = uri.IsDefaultPort ? 443 : uri.Port;

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cancellationToken);

            using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host
            }, cancellationToken);

            if (ssl.RemoteCertificate == null) return null;
            var cert = new X509Certificate2(ssl.RemoteCertificate);
            return cert.NotAfter.ToUniversalTime();
        }
    }
}
