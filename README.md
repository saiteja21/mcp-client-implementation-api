# Enterprise MCP Integration API

![.NET Version](https://img.shields.io/badge/.NET-9.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Build Status](https://img.shields.io/badge/build-passing-brightgreen)

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Core Components](#core-components)
- [Package Dependencies](#package-dependencies)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Getting Started](#getting-started)
- [Development Guidelines](#development-guidelines)
- [Testing](#testing)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

The **Enterprise MCP Integration API** is a production-ready .NET 9.0 Web API that provides seamless integration with Model Context Protocol (MCP) servers to deliver Microsoft documentation search capabilities. Built with enterprise-grade patterns, this API serves as a bridge between client applications and Microsoft Learn's documentation through MCP protocol communication.

### 🌟 Key Features

- **Enterprise-Ready Architecture**: Implements dependency injection, comprehensive logging, and robust error handling
- **Two-File Architecture Pattern**: Optimal separation between reusable core operations and use-case specific logic
- **Microsoft Learn Integration**: Direct access to Microsoft's official documentation through MCP protocol
- **High Performance**: Optimized async/await patterns with minimal thread pool overhead
- **Comprehensive API Documentation**: Full Swagger/OpenAPI documentation with examples
- **Production Monitoring**: Built-in health checks, logging, and metrics collection
- **Scalable Design**: Easily extensible to support additional MCP endpoints

## 🏗️ Architecture

### System Architecture Diagram

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Client Apps   │───▶│   Web API Layer  │───▶│  Service Layer  │
│                 │    │                  │    │                 │
│ • Web Apps      │    │ • Controllers    │    │ • Business      │
│ • Mobile Apps   │    │ • Middleware     │    │   Logic         │
│ • Desktop Apps  │    │ • Validation     │    │ • Data Trans.   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
                       ┌─────────────────┐    ┌─────────────────┐
                       │  Helper Layer   │◀───│  Interface      │
                       │                 │    │  Abstractions   │
                       │ • Static Utils  │    │                 │
                       │ • Data Parsing  │    │ • Service       │
                       │ • Validation    │    │   Contracts     │
                       └─────────────────┘    └─────────────────┘
                                │
                       ┌─────────────────┐
                       │   MCP Protocol  │
                       │                 │
                       │ • SSE Transport │
                       │ • HTTP Client   │
                       │ • Error Handle  │
                       └─────────────────┘
                                │
                       ┌─────────────────┐
                       │ Microsoft Learn │
                       │   MCP Server    │
                       │                 │
                       │ • Documentation │
                       │ • Search API    │
                       │ • Content Mgmt  │
                       └─────────────────┘
```

### Data Flow Architecture

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Request   │───▶│ Controller  │───▶│   Service   │───▶│   Helper    │
│             │    │             │    │             │    │             │
│ • Query     │    │ • Validate  │    │ • Orchestr. │    │ • Utils     │
│ • Headers   │    │ • Route     │    │ • Business  │    │ • Parse     │
│ • Auth      │    │ • Transform │    │   Logic     │    │ • Validate  │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
                                                                   │
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  Response   │◀───│  Transform  │◀───│ MCP Client  │◀───│ MCP Server  │
│             │    │             │    │             │    │             │
│ • JSON      │    │ • Structure │    │ • Protocol  │    │ • Microsoft │
│ • Metadata  │    │ • Format    │    │ • Transport │    │   Learn API │
│ • Status    │    │ • Enrich    │    │ • Process   │    │ • Content   │
└─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘
```

## 📁 Project Structure

```
EnterpriseMcpIntegration/
├── 📁 Controllers/                    # API Controllers
│   └── MicrosoftDocsController.cs    # Microsoft Docs search endpoint
├── 📁 Services/                      # Business Logic Layer
│   ├── McpClientService.cs          # Universal MCP client operations
│   └── MicrosoftDocsService.cs      # Microsoft-specific search logic
├── 📁 Helpers/                      # Static Utility Classes
│   ├── McpClientHelper.cs           # MCP client creation utilities
│   └── MicrosoftDocsHelper.cs       # Microsoft docs processing utils
├── 📁 Interfaces/                   # Service Contracts
│   ├── IMcpClientService.cs         # MCP client interface
│   └── IMicrosoftDocsService.cs     # Microsoft docs service interface
├── 📁 Properties/                   # Launch Settings
│   └── launchSettings.json          # Development environment config
├── 📄 Program.cs                    # Application entry point
├── 📄 appsettings.json             # Configuration settings
├── 📄 appsettings.Development.json # Development overrides
├── 📄 appsettings.Production.json  # Production overrides
└── 📄 EnterpriseMcpIntegration.csproj # Project file
```

## 🔧 Core Components

### 1. **MCP Client Service** (`McpClientService.cs`)
**Purpose**: Universal MCP protocol operations manager

**Responsibilities**:
- Creates and manages MCP client connections
- Handles SSE (Server-Sent Events) and HTTP transport protocols
- Manages client lifecycle (creation, connection testing, disposal)
- Processes MCP responses and extracts content
- Provides connection pooling and retry mechanisms

**Key Methods**:
```csharp
Task<IMcpClient> CreateClientAsync(McpEndpointConfig config)
Task<bool> TestConnectionAsync(string endpointUrl)
Task<IList<McpClientTool>> DiscoverToolsAsync(IMcpClient client)
Task<CallToolResult> CallToolAsync(IMcpClient client, string toolName, Dictionary<string, object?> parameters)
List<string> ProcessResponse(CallToolResult result)
```

### 2. **Microsoft Docs Service** (`MicrosoftDocsService.cs`)
**Purpose**: Microsoft Learn documentation search orchestrator

**Responsibilities**:
- Coordinates Microsoft documentation searches
- Integrates with McpClientService for protocol operations
- Implements query validation and sanitization
- Structures responses into documentation chunks
- Provides enterprise-specific business logic

**Key Methods**:
```csharp
Task<MSDocsResponse> QueryMSDocsAsync(string query)
```

### 3. **MCP Client Helper** (`McpClientHelper.cs`)
**Purpose**: Static utilities for MCP client operations

**Responsibilities**:
- Creates SSE and HTTP clients with proper configuration
- Handles client connection setup and headers
- Provides test configuration generation
- Manages timeout and retry settings

**Key Methods**:
```csharp
static Task<IMcpClient> CreateSseClientAsync(McpEndpointConfig config, ILogger logger)
static Task<IMcpClient> CreateHttpClientAsync(McpEndpointConfig config, ILogger logger)
static McpEndpointConfig CreateTestConfiguration(string endpointUrl)
```

### 4. **Microsoft Docs Helper** (`MicrosoftDocsHelper.cs`)
**Purpose**: Static utilities for Microsoft documentation processing

**Responsibilities**:
- Validates and sanitizes search queries
- Creates Microsoft Learn endpoint configurations
- Processes JSON responses into structured objects
- Handles documentation chunk parsing and formatting

**Key Methods**:
```csharp
static string ValidateAndSanitizeQuery(string query, ILogger logger)
static McpEndpointConfig CreateMicrosoftLearnConfig(string endpointUrl)
static MSDocsResponse ProcessMSDocsResponse(List<string> rawResponses, string originalQuery, string endpoint, ILogger logger)
static DocumentationChunk? ParseJsonToDocumentationChunk(JsonElement item, string endpoint, ILogger logger)
```

### 5. **Microsoft Docs Controller** (`MicrosoftDocsController.cs`)
**Purpose**: RESTful API endpoint for documentation searches

**Responsibilities**:
- Handles HTTP POST requests for documentation search
- Validates request payloads
- Returns structured JSON responses
- Implements proper HTTP status codes and error handling

**Endpoints**:
```http
POST /api/msdocsping
Content-Type: application/json
{
  "query": "search terms"
}
```

## 📦 Package Dependencies

### Core Framework
- **Microsoft.NET.Sdk.Web** (9.0): ASP.NET Core web framework
- **TargetFramework**: net9.0 with nullable reference types enabled

### MCP Integration
- **ModelContextProtocol** (0.3.0-preview.3): Main MCP protocol implementation
- **ModelContextProtocol.Core** (0.3.0-preview.3): Core MCP functionality and types

### API Documentation
- **Microsoft.AspNetCore.OpenApi** (9.0.7): OpenAPI support for .NET 9
- **Swashbuckle.AspNetCore** (9.0.3): Swagger UI and documentation generation

### Built-in Dependencies
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **Microsoft.Extensions.Logging**: Structured logging framework
- **Microsoft.Extensions.Configuration**: Configuration management
- **System.Text.Json**: High-performance JSON serialization
- **Microsoft.Extensions.Http**: HTTP client factory and configuration

## ⚙️ Configuration

### Application Settings (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "EnterpriseMcpIntegration": "Debug"
    }
  },
  "AllowedHosts": "*",
  "MicrosoftDocs": {
    "EndpointUrl": "https://learn.microsoft.com/api/mcp",
    "EnableCaching": true,
    "CacheExpiryMinutes": 30,
    "Timeout": "00:05:00",
    "MaxRetryAttempts": 3
  },
  "Enterprise": {
    "ServiceName": "Enterprise MCP Integration API",
    "Version": "1.0.0",
    "Environment": "Development"
  }
}
```

### Configuration Sections

#### **MicrosoftDocs Section**
- `EndpointUrl`: Microsoft Learn MCP server endpoint
- `EnableCaching`: Feature flag for response caching
- `CacheExpiryMinutes`: Cache duration in minutes
- `Timeout`: Request timeout duration
- `MaxRetryAttempts`: Number of retry attempts for failed requests

#### **Logging Section**
- Configures log levels for different namespaces
- Supports Console and Debug providers
- Environment-specific overrides available

#### **Enterprise Section**
- Service metadata and versioning information
- Environment identification for deployment tracking

## 🔌 API Endpoints

### Microsoft Documentation Search

**Endpoint**: `POST /api/msdocsping`

**Request Body**:
```json
{
  "query": "What is dependency injection in .NET"
}
```

**Response Format**:
```json
{
  "query": "What is dependency injection in .NET",
  "searchTimestamp": "2025-07-24T12:00:00Z",
  "documentationChunks": [
    {
      "title": "Dependency injection in .NET",
      "content": "Detailed explanation of dependency injection...",
      "contentUrl": "https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection",
      "timestamp": "2025-07-24T12:00:00Z",
      "metadata": {
        "source": "Microsoft Learn",
        "category": ".NET Core"
      }
    }
  ],
  "totalChunks": 5,
  "totalCharacters": 15420,
  "responseSource": "Microsoft Learn MCP Server",
  "errorMessage": null
}
```

**Status Codes**:
- `200 OK`: Successful search with results
- `400 Bad Request`: Invalid request payload or empty query
- `500 Internal Server Error`: Server-side processing error

### Health Check Endpoint

**Endpoint**: `GET /health`
- Returns API health status and version information
- Includes dependency health checks

### Swagger Documentation

**Endpoint**: `GET /swagger`
- Interactive API documentation
- Request/response examples
- Schema definitions

## 🚀 Getting Started

### Prerequisites

- **.NET 9.0 SDK** or later
- **Visual Studio 2022** or **VS Code** with C# extension
- **Git** for version control

### Installation

1. **Clone the Repository**
```bash
git clone https://github.com/your-org/enterprise-mcp-integration.git
cd enterprise-mcp-integration
```

2. **Restore Dependencies**
```bash
dotnet restore
```

3. **Build the Project**
```bash
dotnet build
```

4. **Run the Application**
```bash
dotnet run
```

5. **Access the API**
- API Base URL: `http://localhost:5220`
- Swagger UI: `http://localhost:5220/swagger`
- Health Check: `http://localhost:5220/health`

### Quick Test

```bash
# Test the Microsoft Docs search endpoint
curl -X POST "http://localhost:5220/api/msdocsping" \
  -H "Content-Type: application/json" \
  -d '{"query": "async await best practices"}'
```

Or using PowerShell:
```powershell
Invoke-RestMethod -Uri "http://localhost:5220/api/msdocsping" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"query": "async await best practices"}'
```

## 👨‍💻 Development Guidelines

### Code Organization Principles

#### **Two-File Architecture Pattern**
- **Core Services**: Universal, reusable operations (e.g., `McpClientService`)
- **Use-Case Services**: Specific business logic (e.g., `MicrosoftDocsService`)
- **Static Helpers**: Utility functions for common operations

#### **Dependency Injection Pattern**
```csharp
// Service Registration in Program.cs
builder.Services.AddScoped<IMcpClientService, McpClientService>();
builder.Services.AddScoped<IMicrosoftDocsService, MicrosoftDocsService>();

// Constructor Injection
public MicrosoftDocsService(
    IMcpClientService mcpClient,
    IConfiguration configuration,
    ILogger<MicrosoftDocsService> logger)
{
    _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
    // ... other initializations
}
```

### Performance Optimization

#### **Async/Await Best Practices**
- Use `async`/`await` only for I/O operations
- CPU-bound operations run synchronously to avoid thread pool overhead
- Proper resource disposal with `using` statements and `IDisposable`

#### **Memory Management**
- Minimize object allocations in hot paths
- Use `StringBuilder` for string concatenation
- Implement proper disposal patterns for MCP clients

### Error Handling Strategy

#### **Structured Exception Handling**
```csharp
try
{
    // Business logic
}
catch (ArgumentException ex)
{
    _logger.LogWarning(ex, "Invalid input provided");
    throw new ACEAPIMCPClientException("Invalid request", ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    throw new ACEAPIMCPClientException("Internal error", ex);
}
```

#### **Custom Exception Types**
- `ACEAPIMCPClientException`: MCP-specific errors
- Proper error propagation with inner exceptions
- Structured logging with correlation IDs

### Adding New MCP Endpoints

1. **Create Endpoint-Specific Service**
```csharp
public class NewMcpService : INewMcpService
{
    private readonly IMcpClientService _mcpClient;
    
    // Implementation using existing McpClientService
}
```

2. **Create Helper Class**
```csharp
public static class NewMcpHelper
{
    public static McpEndpointConfig CreateConfig(string endpoint) { }
    public static T ProcessResponse<T>(List<string> responses) { }
}
```

3. **Register Services**
```csharp
builder.Services.AddScoped<INewMcpService, NewMcpService>();
```

4. **Create Controller**
```csharp
[ApiController]
[Route("api/new-endpoint")]
public class NewMcpController : ControllerBase { }
```

## 🧪 Testing

### Unit Testing Strategy

#### **Service Layer Testing**
```csharp
[Test]
public async Task QueryMSDocsAsync_ValidQuery_ReturnsResults()
{
    // Arrange
    var mockMcpClient = new Mock<IMcpClientService>();
    var mockLogger = new Mock<ILogger<MicrosoftDocsService>>();
    var service = new MicrosoftDocsService(mockMcpClient.Object, config, mockLogger.Object);
    
    // Act
    var result = await service.QueryMSDocsAsync("test query");
    
    // Assert
    Assert.IsNotNull(result);
    Assert.IsTrue(result.DocumentationChunks.Any());
}
```

#### **Integration Testing**
```csharp
[Test]
public async Task MsdocsPing_EndToEnd_ReturnsValidResponse()
{
    // Test full API endpoint with real MCP server
    var client = new TestClient();
    var response = await client.PostAsync("/api/msdocsping", content);
    
    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
}
```

### Manual Testing Scripts

#### **Test Configuration** (`api-tests.http`)
```http
### Test Microsoft Docs Search
POST http://localhost:5220/api/msdocsping
Content-Type: application/json

{
  "query": "dependency injection patterns"
}

### Test Health Check
GET http://localhost:5220/health
```

## 🚀 Deployment

### Development Environment

```bash
# Development with hot reload
dotnet watch run

# Development with specific environment
dotnet run --environment Development
```

### Production Deployment

#### **Docker Containerization**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["EnterpriseMcpIntegration.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseMcpIntegration.dll"]
```

#### **Environment Configuration**
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "EnterpriseMcpIntegration": "Information"
    }
  },
  "MicrosoftDocs": {
    "EndpointUrl": "https://learn.microsoft.com/api/mcp",
    "Timeout": "00:02:00",
    "MaxRetryAttempts": 5
  }
}
```

### Azure App Service Deployment

```bash
# Publish to Azure
dotnet publish -c Release
az webapp deploy --resource-group myResourceGroup --name myApp --src-path ./bin/Release/net9.0/publish
```

## 🔧 Troubleshooting

### Common Issues

#### **MCP Connection Failures**
**Symptoms**: Connection timeout errors, unable to discover tools
**Solutions**:
- Verify `MicrosoftDocs:EndpointUrl` configuration
- Check network connectivity to Microsoft Learn
- Increase timeout values in configuration
- Review logs for specific error messages

#### **JSON Parsing Errors**
**Symptoms**: Serialization exceptions, malformed response data
**Solutions**:
- Check Microsoft Learn API response format changes
- Verify `MSDocsHelper.ParseJsonToDocumentationChunk` method
- Enable debug logging for response inspection

#### **Performance Issues**
**Symptoms**: Slow response times, high memory usage
**Solutions**:
- Monitor thread pool utilization
- Check for memory leaks in MCP client disposal
- Review async/await patterns for bottlenecks
- Enable performance profiling

### Logging and Diagnostics

#### **Enable Detailed Logging**
```json
{
  "Logging": {
    "LogLevel": {
      "EnterpriseMcpIntegration": "Trace",
      "Microsoft.Extensions.Http": "Debug"
    }
  }
}
```

#### **Health Check Monitoring**
```bash
# Check API health
curl http://localhost:5220/health

# Check MCP connection
curl -X POST http://localhost:5220/api/msdocsping \
  -H "Content-Type: application/json" \
  -d '{"query": "test"}'
```

### Support and Contact

- **Development Team**: dev-team@company.com
- **Documentation**: Internal wiki or confluence
- **Issue Tracking**: JIRA or GitHub Issues
- **Code Repository**: GitHub Enterprise or Azure DevOps

---

## 📈 Performance Metrics

### Benchmarks
- **Average Response Time**: < 2 seconds for typical queries
- **Throughput**: 100+ concurrent requests supported
- **Memory Usage**: < 100MB base memory footprint
- **CPU Usage**: < 10% during normal operations

### Monitoring Recommendations
- Implement Application Insights for Azure deployments
- Use Prometheus/Grafana for on-premise monitoring
- Set up alerts for response time thresholds
- Monitor MCP server availability and response times

---

*This documentation is maintained by the Enterprise Development Team. Last updated: July 2025*
