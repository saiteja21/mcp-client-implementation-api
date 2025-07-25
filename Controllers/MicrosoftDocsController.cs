using EnterpriseMcpIntegration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EnterpriseMcpIntegration.Controllers
{
    /// <summary>
    /// Microsoft Documentation Search API Controller
    /// </summary>
    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class MicrosoftDocsController : ControllerBase
    {
        private readonly IMicrosoftDocsService _microsoftDocsService;

        public MicrosoftDocsController(IMicrosoftDocsService microsoftDocsService)
        {
            _microsoftDocsService = microsoftDocsService ?? throw new ArgumentNullException(nameof(microsoftDocsService));
        }

        /// <summary>
        /// Search Microsoft documentation
        /// </summary>
        /// <param name="request">Search request containing the query</param>
        /// <returns>Microsoft documentation search results</returns>
        [HttpPost("msdocsping")]
        [ProducesResponseType(typeof(MSDocsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MSDocsResponse>> MsDocsPing([FromBody] SearchRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Query is required and cannot be empty");
            }

            var result = await _microsoftDocsService.QueryMSDocsAsync(request.Query);
            return Ok(result);
        }

    }

    #region Request Model

    /// <summary>
    /// Simple search request model
    /// </summary>
    public class SearchRequest
    {
        [Required(ErrorMessage = "Query is required")]
        [StringLength(500, ErrorMessage = "Query cannot exceed 500 characters")]
        public string Query { get; set; } = string.Empty;
    }

    #endregion
}
