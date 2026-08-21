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

    [HttpDelete("events/{eventId:guid}/reminders/{reminderId:guid}")]
    public async Task<IActionResult> CancelReminderAsync(
        Guid eventId,
        Guid reminderId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        return await calendarService.CancelReminderAsync(userId, eventId, reminderId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("participant-search")]
    public async Task<ActionResult<IReadOnlyList<CalendarEventMemberResponse>>> SearchUsersAsync(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        return Ok(await calendarService.SearchUsersAsync(query, cancellationToken));
    }

    [HttpGet("events/{eventId:guid}/participant-search")]
    public async Task<ActionResult<IReadOnlyList<CalendarEventMemberResponse>>> SearchParticipantsAsync(
        Guid eventId,
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var results = await calendarService.SearchParticipantsAsync(
            userId,
            eventId,
            query,
            cancellationToken);
        return results is null ? NotFound() : Ok(results);
    }

    [HttpPost("events/{eventId:guid}/participants")]
    public async Task<ActionResult<CalendarEventMemberResponse>> AddParticipantAsync(
        Guid eventId,
        AddEventParticipantRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var participant = await calendarService.AddParticipantAsync(
            userId,
            eventId,
            request.UserId,
            cancellationToken);
        return participant is null ? NotFound() : Ok(participant);
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
