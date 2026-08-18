using IdentityService.Api.DTOs.Calendar;
using IdentityService.Api.Security;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/calendar")]
public sealed class CalendarController(ICalendarService calendarService) : ControllerBase
{
    [HttpPost("events")]
    public async Task<ActionResult<CalendarEventResponse>> CreateEventAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var calendarEvent = await calendarService.CreateAsync(userId, request, cancellationToken);
        return Created($"api/calendar/events/{calendarEvent.Id}", calendarEvent);
    }

    [HttpGet("events/by-day")]
    public async Task<ActionResult<IReadOnlyList<CalendarEventResponse>>> GetEventsByDayAsync(
        [FromQuery] DateTimeOffset? day,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var targetDay = day ?? DateTimeOffset.UtcNow;
        return Ok(await calendarService.GetEventsByDayAsync(userId, targetDay, cancellationToken));
    }
    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<CalendarEventResponse>>> GetEventsAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await calendarService.GetForUserAsync(userId, cancellationToken));
    }

    [HttpDelete("events/{eventId:guid}")]
    public async Task<IActionResult> CancelEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return await calendarService.CancelAsync(userId, eventId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetNotificationsAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await calendarService.GetNotificationsAsync(userId, cancellationToken));
    }

    [HttpPut("notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkNotificationReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return await calendarService.MarkNotificationReadAsync(userId, notificationId, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
