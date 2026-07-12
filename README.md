# Pacaj Web Operations & Client Health Portal

An ASP.NET Core 8 Web API responsible for real-time uptime telemetry, latency benchmarking, and automated TLS/SSL certificate lifecycle monitoring across digital client ecosystems.

---

## Architectural Context

Digital agencies and web operations teams managing multiple client domains need continuous visibility into site reliability and certificate expiration. `Pacaj.Portal` exposes a resilient background probing pipeline that executes non-blocking HTTP handshakes and deep socket TLS certificate validations (`TcpClient` + `SslStream`) to prevent silent outages.

```
                      +-----------------------------+
                      |   Pacaj Monitoring Engine   |
                      +--------------+--------------+
                                     |
               +---------------------+---------------------+
               |                     |                     |
     [HTTP Health Probe]     [TLS/SSL Handshake]   [Telemetry Aggregation]
               |                     |                     |
       (Status & Latency)     (X509 Expiry Date)     (Uptime Statistics)
```

---

## Key Capabilities

- **Low-Overhead Endpoint Probing**: Utilizes `IHttpClientFactory` with `HttpCompletionOption.ResponseHeadersRead` to evaluate server response codes and response time without reading entire payload bodies into memory.
- **TLS/SSL Handshake Inspection**: Directly inspects certificates using .NET's `SslStream` and `X509Certificate2` to calculate precise days-to-expiration without relying on third-party scrapers.
- **RESTful Telemetry API**:
  - `GET /api/v1/monitoring/projects`: Lists configured client web applications.
  - `GET /api/v1/monitoring/summary`: Aggregates fleet-wide health status and average latency.
  - `POST /api/v1/monitoring/probe/{id}`: Triggers an immediate health & SSL probe on a target client site.
- **Integrated OpenAPI / Swagger Documentation**: Preconfigured interactive UI available at the root URL.

---

## Technical Stack

- **Framework**: .NET 8.0 (C# 12)
- **Web API**: ASP.NET Core Minimal/Controller API Architecture
- **Security & Cryptography**: `System.Net.Security.SslStream`, `System.Security.Cryptography.X509Certificates`
- **Documentation**: Swashbuckle OpenAPI / Swagger UI

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.

### Build and Run

```bash
# Clone the repository
git clone https://github.com/nastrobot/pacaj-web-portal.git
cd pacaj-web-portal

# Build the project
dotnet build src/Pacaj.Portal/Pacaj.Portal.csproj

# Run the API
dotnet run --project src/Pacaj.Portal/Pacaj.Portal.csproj
```

The Swagger API Explorer will be available at `http://localhost:5000` (or `https://localhost:5001`).

---

## Example Usage

### 1. Fetch Fleet Health Summary
```bash
curl -X GET "https://localhost:5001/api/v1/monitoring/summary" -H "accept: application/json"
```

**Response:**
```json
{
  "totalMonitoredSites": 4,
  "sitesOnline": 4,
  "sitesDegraded": 0,
  "averageLatencyMs": 142.5,
  "timestamp": "2026-10-10T02:40:00Z"
}
```

### 2. Trigger On-Demand Probe
```bash
curl -X POST "https://localhost:5001/api/v1/monitoring/probe/1" -H "accept: application/json"
```

**Response:**
```json
{
  "projectId": 1,
  "url": "https://destinatourism.com",
  "isOnline": true,
  "statusCode": 200,
  "responseTimeMs": 118,
  "checkedAt": "2026-10-10T02:40:05Z",
  "sslValid": true,
  "sslDaysRemaining": 78,
  "errorMessage": null
}
```

---

## Author

**Nasrullah Gullac**  
Full-Stack Software Engineer & Solutions Architect  
Antalya, Turkey  
- GitHub: [@nastrobot](https://github.com/nastrobot)  
- LinkedIn: [linkedin.com/in/nasrullah-gullac](https://linkedin.com/in/nasrullah-gullac)
