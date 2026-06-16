using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;

namespace Habitu.Api.Controllers;

[Authorize]
public class ProfileController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupabaseStorageService _storageService;

    public ProfileController(IApplicationDbContext context, ICurrentUserService currentUserService, ISupabaseStorageService storageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileResponseDto>> GetProfile()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (profile == null) return NotFound("Profile not found");

        return Ok(new ProfileResponseDto(
            profile.Id,
            profile.FullName,
            profile.AvatarUrl,
            profile.Bio,
            profile.AcademicProgram,
            profile.UniversityHeadquarters,
            profile.CreatedAt
        ));
    }

    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequestDto request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (profile == null) return NotFound("Profile not found");

        profile.FullName = request.FullName;
        profile.Bio = request.Bio;
        profile.AcademicProgram = request.AcademicProgram;
        profile.UniversityHeadquarters = request.UniversityHeadquarters;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("upload-photo")]
    public async Task<ActionResult<string>> UploadPhoto(IFormFile file)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == userId);

        if (profile == null) return NotFound("Profile not found");

        using var fileStream = file.OpenReadStream();
        var uniqueFileName = $"{userId.Value}/avatar_{Guid.NewGuid()}_{file.FileName}";
        var avatarUrl = await _storageService.UploadFileAsync("avatars", uniqueFileName, fileStream, file.ContentType);

        profile.AvatarUrl = avatarUrl;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(avatarUrl);
    }
}