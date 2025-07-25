using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace EnterpriseMcpIntegration.Interfaces
{
    /// <summary>
    /// Interface for Universal MCP Client Service operations
    /// Defines all MCP protocol operations that can be reused across different endpoints
    /// </summary>
    public interface IMcpClientService : IDisposable
    {
        /// <summary>
        /// Creates and configures an MCP client for the specified endpoint
        /// </summary>
        /// <param name="config">Endpoint configuration</param>
        /// <returns>Configured MCP client</returns>
        Task<IMcpClient> CreateClientAsync(McpEndpointConfig config);

        /// <summary>
        /// Tests connection to an MCP endpoint
        /// </summary>
        /// <param name="endpointUrl">Endpoint URL to test</param>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(string endpointUrl);

        /// <summary>
        /// Discovers available tools from an MCP client
        /// </summary>
        /// <param name="client">MCP client instance</param>
        /// <returns>List of available tools</returns>
        Task<IList<McpClientTool>> DiscoverToolsAsync(IMcpClient client);

        /// <summary>
        /// Calls a specific tool on the MCP client
        /// </summary>
        /// <param name="client">MCP client instance</param>
        /// <param name="toolName">Name of the tool to call</param>
        /// <param name="parameters">Tool parameters</param>
        /// <returns>Tool execution result</returns>
        Task<CallToolResult> CallToolAsync(IMcpClient client, string toolName, Dictionary<string, object?> parameters);

        /// <summary>
        /// Processes raw MCP response and extracts text content
        /// </summary>
        /// <param name="result">MCP tool call result</param>
        /// <returns>Processed response content</returns>
        List<string> ProcessResponse(CallToolResult result);

        /// <summary>
        /// Registers an endpoint configuration for later use
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <param name="config">Endpoint configuration</param>
        void RegisterEndpoint(string name, McpEndpointConfig config);

        /// <summary>
        /// Gets a registered endpoint configuration
        /// </summary>
        /// <param name="name">Endpoint name</param>
        /// <returns>Endpoint configuration or null if not found</returns>
        McpEndpointConfig? GetEndpointConfig(string name);

        /// <summary>
        /// Gets all registered endpoint configurations
        /// </summary>
        /// <returns>Dictionary of all endpoint configurations</returns>
        Dictionary<string, McpEndpointConfig> GetAllEndpointConfigs();
    }

    #region Supporting Types for MCP Client Service

    /// <summary>
    /// Configuration for MCP endpoint connections
    /// </summary>
    public class McpEndpointConfig
    {
        public string Name { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = string.Empty;
        public McpTransportType TransportType { get; set; } = McpTransportType.SSE;
        public string? UserAgent { get; set; }
        public Dictionary<string, string>? CustomHeaders { get; set; }
        public TimeSpan? Timeout { get; set; }
        public bool EnableRetry { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    /// <summary>
    /// Supported MCP transport types
    /// </summary>
    public enum McpTransportType
    {
        SSE,        // Server-Sent Events (default)
        HTTP,       // Standard HTTP
        WebSocket   // WebSocket (future)
    }

    /// <summary>
    /// Custom exception for Enterprise MCP-related errors
    /// </summary>
    public class ACEAPIMCPClientException : Exception
    {
        public string? ErrorCode { get; set; }

        public ACEAPIMCPClientException(string message) : base(message) { }
        public ACEAPIMCPClientException(string message, Exception innerException) : base(message, innerException) { }
        public ACEAPIMCPClientException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    #endregion
}
