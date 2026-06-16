using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Habitu.Application.DTOs;
using Habitu.Application.Features.Auth.Commands;

namespace Habitu.Api.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        var result = await Mediator.Send(new RegisterCommand(request));
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var result = await Mediator.Send(new LoginCommand(request));
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await Mediator.Send(new RefreshTokenCommand(request));
        return Ok(result);
    }
}