using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol;
using System.Collections.Concurrent;
using EnterpriseMcpIntegration.Interfaces;
using EnterpriseMcpIntegration.Helpers;

namespace EnterpriseMcpIntegration.Services
{
    /// <summary>
    /// Universal MCP Client Service - handles all MCP protocol operations
    /// Reusable across different MCP endpoints and use cases
    /// </summary>
    public class McpClientService : IMcpClientService
    {
        private readonly ILogger<McpClientService> _logger;
        private readonly ConcurrentDictionary<string, IMcpClient> _activeClients;
        private readonly ConcurrentDictionary<string, McpEndpointConfig> _endpointConfigs;
        private bool _disposed = false;

        public McpClientService(ILogger<McpClientService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeClients = new ConcurrentDictionary<string, IMcpClient>();
            _endpointConfigs = new ConcurrentDictionary<string, McpEndpointConfig>();
        }

        /// <summary>
        /// Creates and configures an MCP client for the specified endpoint
        /// </summary>
        /// <param name="config">Endpoint configuration</param>
        /// <returns>Configured MCP client</returns>
        public async Task<IMcpClient> CreateClientAsync(McpEndpointConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            try
            {
                _logger.LogInformation("Creating MCP client for endpoint: {EndpointUrl}", config.EndpointUrl);

                // Check if client already exists
                if (_activeClients.TryGetValue(config.Name, out var existingClient))
                {
                    _logger.LogDebug("Reusing existing client for: {EndpointName}", config.Name);
                    return existingClient;
                }

                // Create new client based on transport type
                var client = config.TransportType switch
                {
                    McpTransportType.SSE => await McpClientHelper.CreateSseClientAsync(config, _logger),
                    McpTransportType.HTTP => await McpClientHelper.CreateHttpClientAsync(config, _logger),
                    _ => throw new NotSupportedException($"Transport type {config.TransportType} is not supported")
                };

                // Cache the client and config
                _activeClients.TryAdd(config.Name, client);
                _endpointConfigs.TryAdd(config.Name, config);

                _logger.LogInformation("Successfully created MCP client for: {EndpointName}", config.Name);
                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MCP client for endpoint: {EndpointUrl}", config.EndpointUrl);
                throw new ACEAPIMCPClientException($"Failed to create MCP client: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tests connection to an MCP endpoint
        /// </summary>
        /// <param name="endpointUrl">Endpoint URL to test</param>
        /// <returns>True if connection is successful</returns>
        public async Task<bool> TestConnectionAsync(string endpointUrl)
        {
            try
            {
                var testConfig = McpClientHelper.CreateTestConfiguration(endpointUrl);
                var testClient = await McpClientHelper.CreateSseClientAsync(testConfig, _logger);
                var tools = await testClient.ListToolsAsync();
                
                _logger.LogInformation("Connection test successful for: {EndpointUrl}. Found {ToolCount} tools", 
                    endpointUrl, tools.Count);
                
                // Dispose the test client
                if (testClient is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection test failed for: {EndpointUrl}", endpointUrl);
                return false;
            }
        }

        /// <summary>
        /// Discovers available tools from an MCP client
        /// </summary>
        /// <param name="client">MCP client instance</param>
        /// <returns>List of available tools</returns>
        public async Task<IList<McpClientTool>> DiscoverToolsAsync(IMcpClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            try
            {
                _logger.LogDebug("Discovering tools from MCP client");
                var tools = await client.ListToolsAsync();
                
                _logger.LogInformation("Discovered {ToolCount} tools from MCP endpoint", tools.Count);
                return tools;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover tools from MCP client");
                throw new ACEAPIMCPClientException($"Tool discovery failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calls a specific tool on the MCP client
        /// </summary>
        /// <param name="client">MCP client instance</param>
        /// <param name="toolName">Name of the tool to call</param>
        /// <param name="parameters">Tool parameters</param>
        /// <returns>Tool execution result</returns>
        public async Task<CallToolResult> CallToolAsync(IMcpClient client, string toolName, 
            Dictionary<string, object?> parameters)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (string.IsNullOrWhiteSpace(toolName)) throw new ArgumentException("Tool name cannot be empty", nameof(toolName));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            try
            {
                _logger.LogDebug("Calling tool {ToolName} with {ParameterCount} parameters", 
                    toolName, parameters.Count);

                var result = await client.CallToolAsync(toolName, parameters);
                
                _logger.LogInformation("Successfully called tool {ToolName}. Response contains {ContentCount} content blocks", 
                    toolName, result.Content?.Count ?? 0);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call tool {ToolName}", toolName);
                throw new ACEAPIMCPClientException($"Tool call failed for '{toolName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes raw MCP response and extracts text content
        /// </summary>
        /// <param name="result">MCP tool call result</param>
        /// <returns>Processed response content</returns>
        public List<string> ProcessResponse(CallToolResult result)
        {
            if (result?.Content == null) return new List<string>();

            var responses = new List<string>();

            try
            {
                foreach (var content in result.Content)
                {
                    if (content is TextContentBlock textBlock && 
                        !string.IsNullOrWhiteSpace(textBlock.Text) &&
                        !textBlock.Text.StartsWith("An error occurred", StringComparison.OrdinalIgnoreCase))
                    {
                        responses.Add(textBlock.Text);
                        _logger.LogDebug("Processed content block with {CharacterCount} characters", 
                            textBlock.Text.Length);
                    }
                }

                _logger.LogInformation("Processed {ResponseCount} valid content blocks from MCP response", 
                    responses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process MCP response");
                throw new ACEAPIMCPClientException($"Response processing failed: {ex.Message}", ex);
            }

            return responses;
        }

        /// <summary>
        /// Registers an endpoint configuration for later use
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <param name="config">Endpoint configuration</param>
        public void RegisterEndpoint(string name, McpEndpointConfig config)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Endpoint name cannot be empty", nameof(name));
            if (config == null) throw new ArgumentNullException(nameof(config));

            config.Name = name;
            _endpointConfigs.AddOrUpdate(name, config, (key, oldValue) => config);
            
            _logger.LogInformation("Registered endpoint configuration: {EndpointName} -> {EndpointUrl}", 
                name, config.EndpointUrl);
        }

        /// <summary>
        /// Gets a registered endpoint configuration
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <returns>Endpoint configuration or null if not found</returns>
        public McpEndpointConfig? GetEndpointConfig(string name)
        {
            return _endpointConfigs.TryGetValue(name, out var config) ? config : null;
        }

        /// <summary>
        /// Gets all registered endpoint configurations
        /// </summary>
        /// <returns>Dictionary of all endpoint configurations</returns>
        public Dictionary<string, McpEndpointConfig> GetAllEndpointConfigs()
        {
            return new Dictionary<string, McpEndpointConfig>(_endpointConfigs);
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var client in _activeClients.Values)
                {
                    if (client is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error disposing MCP client");
                        }
                    }
                }

                _activeClients.Clear();
                _endpointConfigs.Clear();
                _disposed = true;
                
                _logger.LogInformation("McpClientService disposed successfully");
            }
        }

        #endregion
    }
}
