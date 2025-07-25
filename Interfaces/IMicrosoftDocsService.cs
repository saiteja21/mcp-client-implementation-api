using System.ComponentModel.DataAnnotations;

namespace EnterpriseMcpIntegration.Interfaces
{
    /// <summary>
    /// Interface for Microsoft Documentation Search Service operations
    /// Defines methods for searching and retrieving Microsoft Learn documentation
    /// </summary>
    public interface IMicrosoftDocsService
    {
        /// <summary>
        /// Searches Microsoft documentation using natural language query
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <returns>Structured Microsoft documentation response</returns>
        Task<MSDocsResponse> QueryMSDocsAsync(string query);
    }

    #region Supporting Types for Microsoft Docs Service

    /// <summary>
    /// Response structure for Microsoft documentation searches
    /// </summary>
    public class MSDocsResponse
    {
        public string Query { get; set; } = string.Empty;
        public DateTime SearchTimestamp { get; set; }
        public List<DocumentationChunk> DocumentationChunks { get; set; } = new List<DocumentationChunk>();
        public int TotalChunks { get; set; }
        public int TotalCharacters { get; set; }
        public string ResponseSource { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual documentation content chunk
    /// </summary>
    public class DocumentationChunk
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentUrl { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Search request model for API endpoints
    /// </summary>
    public class SearchRequest
    {
        [Required(ErrorMessage = "Query is required")]
        [StringLength(500, ErrorMessage = "Query cannot exceed 500 characters")]
        public string Query { get; set; } = string.Empty;
    }

    #endregion
}
