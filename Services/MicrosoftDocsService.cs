using EnterpriseMcpIntegration.Interfaces;
using EnterpriseMcpIntegration.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EnterpriseMcpIntegration.Services
{
    /// <summary>
    /// Microsoft Documentation Search Service
    /// Uses IMcpClientService to interact with Microsoft Learn MCP Server
    /// </summary>
    public class MicrosoftDocsService : IMicrosoftDocsService
    {
        private readonly IMcpClientService _mcpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MicrosoftDocsService> _logger;
        private readonly string _microsoftLearnEndpoint;

        public MicrosoftDocsService(
            IMcpClientService mcpClient, 
            IConfiguration configuration,
            ILogger<MicrosoftDocsService> logger)
        {
            _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _microsoftLearnEndpoint = _configuration["MicrosoftDocs:EndpointUrl"] 
                ?? "https://learn.microsoft.com/api/mcp";
        }

        /// <summary>
        /// Searches Microsoft documentation using natural language query
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <returns>Structured Microsoft documentation response</returns>
        public async Task<MSDocsResponse> QueryMSDocsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Search query cannot be empty", nameof(query));
            }

            try
            {
                _logger.LogInformation("Starting Microsoft Docs search for query: {Query}", query);

                // Validate and sanitize the query
                var sanitizedQuery = MSDocsHelper.ValidateAndSanitizeQuery(query, _logger);

                // Create MCP client for Microsoft Learn
                var endpointConfig = MSDocsHelper.CreateMicrosoftLearnConfig(_microsoftLearnEndpoint);
                var mcpClient = await _mcpClient.CreateClientAsync(endpointConfig);

                // Discover available tools
                var tools = await _mcpClient.DiscoverToolsAsync(mcpClient);
                var toolName = MSDocsHelper.GetMicrosoftDocsToolName();
                if (!tools.Any(t => t.Name == toolName))
                {
                    throw new InvalidOperationException($"Microsoft Docs search tool '{toolName}' is not available");
                }

                // Execute the search
                var parameters = new Dictionary<string, object?> { { "question", sanitizedQuery } };
                var result = await _mcpClient.CallToolAsync(mcpClient, toolName, parameters);

                // Process the response
                var rawResponses = _mcpClient.ProcessResponse(result);
                var docsResponse = MSDocsHelper.ProcessMSDocsResponse(rawResponses, sanitizedQuery, _microsoftLearnEndpoint, _logger);

                _logger.LogInformation("Successfully completed Microsoft Docs search. Found {ChunkCount} documentation chunks", 
                    docsResponse.DocumentationChunks.Count);

                return docsResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search Microsoft documentation for query: {Query}", query);
                throw new ACEAPIMCPClientException($"Microsoft Docs search failed: {ex.Message}", ex);
            }
        }
    }
}
