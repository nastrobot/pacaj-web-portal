using Microsoft.AspNetCore.Mvc;
using Pacaj.Portal.Models;
using Pacaj.Portal.Services;

namespace Pacaj.Portal.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class MonitoringController : ControllerBase
    {
        private readonly ISiteMonitoringService _monitoringService;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            ISiteMonitoringService monitoringService,
            ILogger<MonitoringController> logger)
        {
            _monitoringService = monitoringService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the list of all configured client web systems and their baseline status.
        /// </summary>
        [HttpGet("projects")]
        [ProducesResponseType(typeof(IEnumerable<ClientProject>), StatusCodes.Status200OK)]
        public IActionResult GetProjects()
        {
            return Ok(_monitoringService.GetRegisteredProjects());
        }

        /// <summary>
        /// Gathers real-time telemetry across all live client projects including HTTP status and SSL validation.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(MonitoringSummary), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGlobalSummary(CancellationToken cancellationToken)
        {
            var summary = await _monitoringService.GetGlobalSummaryAsync(cancellationToken);
            return Ok(summary);
        }

        /// <summary>
        /// Executes an on-demand health and TLS probe against a specific registered project ID.
        /// </summary>
        [HttpPost("probe/{id:int}")]
        [ProducesResponseType(typeof(SiteHealthCheckResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ProbeProject(int id, CancellationToken cancellationToken)
        {
            var project = _monitoringService.GetRegisteredProjects().FirstOrDefault(p => p.Id == id);
            if (project == null)
            {
                return NotFound(new { error = $"Project with ID {id} was not found in the monitoring registry." });
            }

            _logger.LogInformation("Initiating manual probe for project '{Title}' ({Url})", project.ProjectTitle, project.LiveUrl);
            var result = await _monitoringService.CheckSiteAsync(project, cancellationToken);
            return Ok(result);
        }
    }
}
