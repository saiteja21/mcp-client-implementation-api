using Microsoft.Extensions.Logging;
using System.Text.Json;
using EnterpriseMcpIntegration.Interfaces;

namespace EnterpriseMcpIntegration.Helpers
{
    /// <summary>
    /// Static helper class for Microsoft Docs service operations
    /// Contains reusable utility methods extracted from MicrosoftDocsService
    /// </summary>
    public static class MSDocsHelper
    {
        private const string MICROSOFT_DOCS_TOOL_NAME = "microsoft_docs_search";

        /// <summary>
        /// Creates Microsoft Learn endpoint configuration
        /// </summary>
        /// <param name="endpointUrl">Microsoft Learn endpoint URL</param>
        /// <returns>Configured MCP endpoint configuration</returns>
        public static McpEndpointConfig CreateMicrosoftLearnConfig(string endpointUrl)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new ArgumentException("Endpoint URL cannot be empty", nameof(endpointUrl));

            return new McpEndpointConfig
            {
                Name = "MicrosoftLearn",
                EndpointUrl = endpointUrl,
                TransportType = McpTransportType.SSE,
                UserAgent = "Enterprise Microsoft Docs MCP Client/1.0",
                Timeout = TimeSpan.FromMinutes(5),
                EnableRetry = true,
                MaxRetryAttempts = 3
            };
        }

        /// <summary>
        /// Validates and sanitizes user search query
        /// </summary>
        /// <param name="query">Raw search query</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <returns>Sanitized query string</returns>
        public static string ValidateAndSanitizeQuery(string query, ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Query cannot be empty", nameof(query));
            }

            // Basic sanitization
            var sanitized = query.Trim();
            
            // Remove potentially harmful characters
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[<>""']", "");
            
            // Limit query length
            if (sanitized.Length > 500)
            {
                sanitized = sanitized.Substring(0, 500);
                logger.LogWarning("Query was truncated to 500 characters");
            }

            logger.LogDebug("Query sanitized. Original length: {OriginalLength}, Sanitized length: {SanitizedLength}", 
                query.Length, sanitized.Length);

            return sanitized;
        }

        /// <summary>
        /// Processes Microsoft documentation response and converts to structured format
        /// </summary>
        /// <param name="rawResponses">Raw response strings from MCP call</param>
        /// <param name="originalQuery">Original search query</param>
        /// <param name="microsoftLearnEndpoint">Microsoft Learn endpoint URL</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <returns>Processed Microsoft documentation response</returns>
        public static MSDocsResponse ProcessMSDocsResponse(
            List<string> rawResponses, 
            string originalQuery, 
            string microsoftLearnEndpoint, 
            ILogger logger)
        {
            if (rawResponses == null) throw new ArgumentNullException(nameof(rawResponses));
            if (string.IsNullOrWhiteSpace(originalQuery)) throw new ArgumentException("Original query cannot be empty", nameof(originalQuery));
            if (string.IsNullOrWhiteSpace(microsoftLearnEndpoint)) throw new ArgumentException("Microsoft Learn endpoint cannot be empty", nameof(microsoftLearnEndpoint));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            var documentationChunks = new List<DocumentationChunk>();
            var totalCharacters = 0;

            logger.LogDebug("Processing {ResponseCount} raw responses", rawResponses.Count);

            foreach (var rawResponse in rawResponses)
            {
                try
                {
                    // Try to parse as JSON array (Microsoft's typical response format)
                    var jsonArray = JsonSerializer.Deserialize<JsonElement[]>(rawResponse);
                    
                    if (jsonArray != null)
                    {
                        foreach (var item in jsonArray)
                        {
                            var chunk = ParseJsonToDocumentationChunk(item, microsoftLearnEndpoint, logger);
                            if (chunk != null)
                            {
                                documentationChunks.Add(chunk);
                                totalCharacters += chunk.Content?.Length ?? 0;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, treat as plain text
                    var chunk = new DocumentationChunk
                    {
                        Title = "Microsoft Documentation",
                        Content = rawResponse,
                        ContentUrl = microsoftLearnEndpoint,
                        Timestamp = DateTime.UtcNow
                    };
                    documentationChunks.Add(chunk);
                    totalCharacters += rawResponse.Length;
                    
                    logger.LogDebug("Response treated as plain text. Length: {Length}", rawResponse.Length);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to process response chunk, skipping");
                }
            }

            logger.LogInformation("Processed {ChunkCount} documentation chunks with {TotalCharacters} total characters", 
                documentationChunks.Count, totalCharacters);

            return new MSDocsResponse
            {
                Query = originalQuery,
                SearchTimestamp = DateTime.UtcNow,
                DocumentationChunks = documentationChunks,
                TotalChunks = documentationChunks.Count,
                TotalCharacters = totalCharacters,
                ResponseSource = "Microsoft Learn MCP Server"
            };
        }

        /// <summary>
        /// Parses JSON element to documentation chunk
        /// </summary>
        /// <param name="jsonElement">JSON element to parse</param>
        /// <param name="microsoftLearnEndpoint">Default endpoint URL</param>
        /// <param name="logger">Logger instance for logging operations</param>
        /// <returns>Parsed documentation chunk or null if parsing fails</returns>
        public static DocumentationChunk? ParseJsonToDocumentationChunk(
            JsonElement jsonElement, 
            string microsoftLearnEndpoint, 
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(microsoftLearnEndpoint)) throw new ArgumentException("Microsoft Learn endpoint cannot be empty", nameof(microsoftLearnEndpoint));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            try
            {
                var title = GetJsonProperty(jsonElement, "title");
                var content = GetJsonProperty(jsonElement, "content");
                var contentUrl = GetJsonProperty(jsonElement, "contentUrl");

                if (string.IsNullOrEmpty(content))
                {
                    logger.LogDebug("Skipping JSON element - no content found");
                    return null;
                }

                var chunk = new DocumentationChunk
                {
                    Title = title ?? "Microsoft Documentation",
                    Content = content,
                    ContentUrl = contentUrl ?? microsoftLearnEndpoint,
                    Timestamp = DateTime.UtcNow
                };

                logger.LogDebug("Parsed documentation chunk: {Title}, Content length: {ContentLength}", 
                    chunk.Title, chunk.Content.Length);

                return chunk;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse JSON element to documentation chunk");
                return null;
            }
        }

        /// <summary>
        /// Extracts string property from JSON element
        /// </summary>
        /// <param name="element">JSON element to extract from</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <returns>Property value as string or null if not found</returns>
        public static string? GetJsonProperty(JsonElement element, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name cannot be empty", nameof(propertyName));

            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;
        }

        /// <summary>
        /// Gets the Microsoft Docs tool name constant
        /// </summary>
        /// <returns>Microsoft Docs tool name</returns>
        public static string GetMicrosoftDocsToolName()
        {
            return MICROSOFT_DOCS_TOOL_NAME;
        }
    }
}
