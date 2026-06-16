using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Habitu.Application.DTOs;
using Habitu.Application.Features.Sync.Commands;

namespace Habitu.Api.Controllers;

[Authorize]
public class SyncController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SyncResponseDto>> Synchronize([FromBody] SyncRequestDto request)
    {
        var result = await Mediator.Send(new SynchronizeDataCommand(request));
        return Ok(result);
    }
}