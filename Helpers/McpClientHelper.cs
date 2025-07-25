using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using EnterpriseMcpIntegration.Interfaces;

namespace EnterpriseMcpIntegration.Helpers
{
    /// <summary>
    /// Static helper class for MCP client transport creation and configuration
    /// Contains reusable utility methods extracted from McpClientService
    /// </summary>
    public static class McpClientHelper
    {
        /// <summary>
        /// Creates and configures an SSE-based MCP client transport
        /// </summary>
        /// <param name="config">Endpoint configuration</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <returns>Configured MCP client</returns>
        public static async Task<IMcpClient> CreateSseClientAsync(McpEndpointConfig config, ILogger logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            try
            {
                logger.LogDebug("Creating SSE client for endpoint: {EndpointUrl}", config.EndpointUrl);

                // Configure HTTP client with connection pooling
                var httpHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
                };
                var httpClient = new HttpClient(httpHandler);

                // Set required headers for MCP compatibility
                httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream, application/json");
                httpClient.DefaultRequestHeaders.Add("User-Agent", config.UserAgent ?? "Enterprise MCP Client/1.0");

                // Add custom headers if specified
                if (config.CustomHeaders != null)
                {
                    foreach (var header in config.CustomHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                // Set timeout
                if (config.Timeout.HasValue)
                {
                    httpClient.Timeout = config.Timeout.Value;
                }

                // Create logger factory
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole().SetMinimumLevel(LogLevel.Warning);
                });

                var transport = new SseClientTransport(new()
                {
                    Endpoint = new Uri(config.EndpointUrl),
                    Name = config.Name
                }, httpClient, loggerFactory);

                var client = await McpClientFactory.CreateAsync(transport, loggerFactory: loggerFactory);
                
                logger.LogDebug("Successfully created SSE client for: {EndpointName}", config.Name);
                return client;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create SSE client for endpoint: {EndpointUrl}", config.EndpointUrl);
                throw new ACEAPIMCPClientException($"Failed to create SSE client: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates and configures an HTTP-based MCP client transport
        /// Currently falls back to SSE implementation
        /// </summary>
        /// <param name="config">Endpoint configuration</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <returns>Configured MCP client</returns>
        public static async Task<IMcpClient> CreateHttpClientAsync(McpEndpointConfig config, ILogger logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            logger.LogDebug("Creating HTTP client for endpoint: {EndpointUrl} (falling back to SSE)", config.EndpointUrl);
            
            // HTTP transport implementation would go here
            // For now, fall back to SSE
            return await CreateSseClientAsync(config, logger);
        }

        /// <summary>
        /// Creates a test configuration for connection testing
        /// </summary>
        /// <param name="endpointUrl">Endpoint URL to test</param>
        /// <returns>Test configuration object</returns>
        public static McpEndpointConfig CreateTestConfiguration(string endpointUrl)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl)) 
                throw new ArgumentException("Endpoint URL cannot be empty", nameof(endpointUrl));

            return new McpEndpointConfig
            {
                Name = $"test-{Guid.NewGuid():N}",
                EndpointUrl = endpointUrl,
                TransportType = McpTransportType.SSE,
                Timeout = TimeSpan.FromSeconds(10)
            };
        }
    }
}
