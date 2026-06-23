using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Habitu.Application.Abstractions;
using Habitu.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Habitu.Api.Controllers;

[Authorize]
public class TemplatesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public TemplatesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("goals")]
    public ActionResult<List<TemplateGoalDto>> GetGoals()
    {
        return Ok(GoalTemplateCatalog.Goals);
    }

    [HttpGet]
    public async Task<ActionResult<List<HabitTemplateDto>>> GetTemplates([FromQuery] string? goalKey, CancellationToken cancellationToken)
    {
        var templates = await _context.HabitTemplates
            .Where(t => goalKey == null || t.GoalKey == goalKey)
            .OrderByDescending(t => t.IsFeatured)
            .ThenBy(t => t.Title)
            .Select(t => new HabitTemplateDto(
                t.Id,
                t.Title,
                t.Description,
                t.GoalKey,
                t.Category,
                t.LifestyleTags,
                t.SuggestedFrequencyType,
                t.SuggestedFrequencyDays,
                t.DefaultColorHex,
                t.DefaultIconKey,
                t.IsFeatured
            ))
            .ToListAsync(cancellationToken);

        if (templates.Count > 0)
        {
            return Ok(templates);
        }

        var fallback = GoalTemplateCatalog.Templates
            .Where(t => goalKey == null || t.GoalKey == goalKey)
            .ToList();
        return Ok(fallback);
    }
}
