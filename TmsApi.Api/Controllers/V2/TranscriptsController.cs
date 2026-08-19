using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v2/transcripts")]
public class TranscriptsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        [FromBody] object? request,
        CancellationToken ct)
    {
        await Task.Delay(
            TimeSpan.FromSeconds(5),
            ct);

        return Ok();
    }
}