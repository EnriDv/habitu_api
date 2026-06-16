using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Habitu.Application.DTOs;
using Habitu.Application.Features.Habits.Commands;
using Habitu.Application.Features.Habits.Queries;

namespace Habitu.Api.Controllers;

[Authorize]
public class HabitsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<HabitSyncDto>>> GetActiveHabits()
    {
        var result = await Mediator.Send(new GetActiveHabitsQuery());
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<ActionResult<HabitSyncDto>> Create([FromBody] CreateHabitCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<HabitLogSyncDto>> Complete(
        [FromForm] Guid habitId,
        [FromForm] string executionDate,
        IFormFile? evidenceFile)
    {
        Stream? fileStream = null;
        string? contentType = null;
        string? fileName = null;

        if (evidenceFile != null)
        {
            fileStream = evidenceFile.OpenReadStream();
            contentType = evidenceFile.ContentType;
            fileName = evidenceFile.FileName;
        }

        if (!DateOnly.TryParse(executionDate, out var date))
        {
            date = DateOnly.FromDateTime(DateTime.Today);
        }

        var command = new CompleteHabitCommand(
            habitId,
            date,
            fileStream,
            contentType,
            fileName
        );

        var result = await Mediator.Send(command);
        return Ok(result);
    }
}