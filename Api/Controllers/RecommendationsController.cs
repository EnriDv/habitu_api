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
public class RecommendationsController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RecommendationsController(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<HabitRecommendationDto>>> Get(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return Unauthorized();

        var habits = await _context.Habits
            .Where(h => h.UserId == userId.Value && !h.IsDeleted)
            .Select(h => new
            {
                h.Title,
                Logs = h.HabitLogs.Where(log => !log.IsDeleted).Select(log => log.ExecutionDate).ToList()
            })
            .ToListAsync(cancellationToken);

        var existingTitles = habits
            .Select(h => h.Title.Trim().ToLowerInvariant())
            .ToHashSet();

        var lowConsistency = habits
            .Where(h => h.Logs.Count(date => date >= DateOnly.FromDateTime(System.DateTime.UtcNow.AddDays(-14))) <= 2)
            .Select(h => h.Title)
            .Take(2)
            .ToList();

        var sourceTemplates = await _context.HabitTemplates
            .OrderByDescending(t => t.IsFeatured)
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

        if (sourceTemplates.Count == 0)
        {
            sourceTemplates = GoalTemplateCatalog.Templates.ToList();
        }

        var recommendations = new List<HabitRecommendationDto>();
        foreach (var template in sourceTemplates)
        {
            if (existingTitles.Contains(template.Title.Trim().ToLowerInvariant()))
            {
                continue;
            }

            var reason = habits.Count == 0
                ? "Buen punto de partida para arrancar con hábitos pequeños."
                : lowConsistency.Count > 0
                    ? $"Tus hábitos {string.Join(", ", lowConsistency)} están flojos; conviene una acción fácil y repetible."
                    : "Complementa un área que todavía no estás trabajando de forma explícita.";

            recommendations.Add(new HabitRecommendationDto(
                Id: template.Id.ToString(),
                Type: "template",
                Title: template.Title,
                Description: template.Description ?? "Sugerencia para construir constancia.",
                Reason: reason,
                GoalKey: template.GoalKey,
                SuggestedPayload: new Dictionary<string, object?>
                {
                    ["templateId"] = template.Id,
                    ["title"] = template.Title,
                    ["description"] = template.Description,
                    ["frequencyType"] = template.SuggestedFrequencyType,
                    ["frequencyDays"] = template.SuggestedFrequencyDays,
                    ["colorHex"] = template.DefaultColorHex,
                    ["iconKey"] = template.DefaultIconKey,
                }
            ));

            if (recommendations.Count == 5)
            {
                break;
            }
        }

        return Ok(recommendations);
    }
}
