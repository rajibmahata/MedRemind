using System.Security.Claims;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedRemind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require JWT authentication
public class RemindersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IReminderSchedulingService _schedulingService;
    private readonly ILogger<RemindersController> _logger;

    public RemindersController(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IReminderSchedulingService schedulingService,
        ILogger<RemindersController> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _schedulingService = schedulingService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new reminder
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReminder([FromBody] CreateReminderRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify medication ownership
            var medication = await _unitOfWork.Repository<Medication>()
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && m.UserId == userId);

            if (medication == null)
            {
                return NotFound(new { message = "Medication not found" });
            }

            // Verify voice recording ownership if provided
            if (request.VoiceRecordingId.HasValue)
            {
                var voiceRecording = await _unitOfWork.Repository<VoiceRecording>()
                    .FirstOrDefaultAsync(v => v.Id == request.VoiceRecordingId && v.UserId == userId);

                if (voiceRecording == null)
                {
                    return BadRequest(new { message = "Voice recording not found" });
                }
            }

            // Create reminder
            var reminder = new Reminder
            {
                MedicationId = request.MedicationId,
                VoiceRecordingId = request.VoiceRecordingId,
                ReminderTime = request.ReminderTime,
                IsEnabled = request.IsEnabled,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Reminder>().AddAsync(reminder);
            await _unitOfWork.SaveChangesAsync();

            // Schedule notification if enabled
            if (request.IsEnabled)
            {
                var voiceRecording = request.VoiceRecordingId.HasValue
                    ? await _unitOfWork.Repository<VoiceRecording>().GetByIdAsync(request.VoiceRecordingId.Value)
                    : null;

                var scheduledTime = DateTime.Today.Add(request.ReminderTime);
                if (scheduledTime < DateTime.Now)
                {
                    scheduledTime = scheduledTime.AddDays(1);
                }

                var notificationId = await _notificationService.ScheduleNotificationAsync(
                    medication.Id,
                    scheduledTime,
                    $"Time to take {medication.Name}",
                    $"Take {medication.Dosage} {medication.Unit}",
                    voiceRecording?.FilePath
                );

                reminder.NotificationId = notificationId;
                await _unitOfWork.Repository<Reminder>().UpdateAsync(reminder);
                await _unitOfWork.SaveChangesAsync();
            }

            return Ok(new {
                message = "Reminder created successfully",
                reminderId = reminder.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reminder");
            return StatusCode(500, new { message = "An error occurred while creating reminder" });
        }
    }

    /// <summary>
    /// Create multiple reminders at once
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateMultipleReminders([FromBody] CreateMultipleRemindersRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Verify medication ownership
            var medication = await _unitOfWork.Repository<Medication>()
                .FirstOrDefaultAsync(m => m.Id == request.MedicationId && m.UserId == userId);

            if (medication == null)
            {
                return NotFound(new { message = "Medication not found" });
            }

            // Verify voice recording ownership if provided
            VoiceRecording? voiceRecording = null;
            if (request.VoiceRecordingId.HasValue)
            {
                voiceRecording = await _unitOfWork.Repository<VoiceRecording>()
                    .FirstOrDefaultAsync(v => v.Id == request.VoiceRecordingId && v.UserId == userId);

                if (voiceRecording == null)
                {
                    return BadRequest(new { message = "Voice recording not found" });
                }
            }

            var createdReminders = new List<int>();

            foreach (var time in request.ReminderTimes)
            {
                var reminder = new Reminder
                {
                    MedicationId = request.MedicationId,
                    VoiceRecordingId = request.VoiceRecordingId,
                    ReminderTime = time,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<Reminder>().AddAsync(reminder);
                await _unitOfWork.SaveChangesAsync();

                // Schedule notification
                var scheduledTime = DateTime.Today.Add(time);
                if (scheduledTime < DateTime.Now)
                {
                    scheduledTime = scheduledTime.AddDays(1);
                }

                var notificationId = await _notificationService.ScheduleNotificationAsync(
                    medication.Id,
                    scheduledTime,
                    $"Time to take {medication.Name}",
                    $"Take {medication.Dosage} {medication.Unit}",
                    voiceRecording?.FilePath
                );

                reminder.NotificationId = notificationId;
                await _unitOfWork.Repository<Reminder>().UpdateAsync(reminder);
                await _unitOfWork.SaveChangesAsync();

                createdReminders.Add(reminder.Id);
            }

            return Ok(new {
                message = $"{createdReminders.Count} reminders created successfully",
                reminderIds = createdReminders
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating multiple reminders");
            return StatusCode(500, new { message = "An error occurred while creating reminders" });
        }
    }

    /// <summary>
    /// Get reminders for a medication
    /// </summary>
    [HttpGet("medication/{medicationId}")]
    public async Task<IActionResult> GetMedicationReminders(int medicationId)
    {
        try
        {
            var reminders = await _unitOfWork.Repository<Reminder>().FindAsync(r => r.MedicationId == medicationId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reminders for medication {MedicationId}", medicationId);
            return StatusCode(500, new { message = "An error occurred while retrieving reminders" });
        }
    }

    /// <summary>
    /// Get all reminders for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserReminders(int userId)
    {
        try
        {
            var medications = await _unitOfWork.Repository<Medication>().FindAsync(m => m.UserId == userId && m.IsActive);
            var medicationIds = medications.Select(m => m.Id).ToList();

            var reminders = await _unitOfWork.Repository<Reminder>().FindAsync(r => medicationIds.Contains(r.MedicationId));
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reminders for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving reminders" });
        }
    }

    /// <summary>
    /// Update reminder time
    /// </summary>
    [HttpPut("{id}/time")]
    public async Task<IActionResult> UpdateReminderTime(int id, [FromBody] UpdateReminderTimeRequest request)
    {
        try
        {
            var reminder = await _unitOfWork.Repository<Reminder>().GetByIdAsync(id);
            
            if (reminder == null)
            {
                return NotFound(new { message = "Reminder not found" });
            }

            reminder.ReminderTime = request.NewTime;
            await _unitOfWork.Repository<Reminder>().UpdateAsync(reminder);
            await _unitOfWork.SaveChangesAsync();

            // Reschedule notification if enabled
            if (reminder.IsEnabled && !string.IsNullOrEmpty(reminder.NotificationId))
            {
                var medication = await _unitOfWork.Repository<Medication>().GetByIdAsync(reminder.MedicationId);
                if (medication != null)
                {
                    var scheduledTime = DateTime.Today.Add(request.NewTime);
                    if (scheduledTime < DateTime.Now)
                    {
                        scheduledTime = scheduledTime.AddDays(1);
                    }

                    await _notificationService.RescheduleNotificationAsync(
                        reminder.NotificationId,
                        scheduledTime
                    );
                }
            }

            return Ok(new { message = "Reminder time updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reminder {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating reminder" });
        }
    }

    /// <summary>
    /// Enable/Disable reminder
    /// </summary>
    [HttpPut("{id}/toggle")]
    public async Task<IActionResult> ToggleReminder(int id, [FromBody] ToggleReminderRequest request)
    {
        try
        {
            var reminder = await _unitOfWork.Repository<Reminder>().GetByIdAsync(id);
            
            if (reminder == null)
            {
                return NotFound(new { message = "Reminder not found" });
            }

            reminder.IsEnabled = request.IsEnabled;
            await _unitOfWork.Repository<Reminder>().UpdateAsync(reminder);
            await _unitOfWork.SaveChangesAsync();

            // Schedule or cancel notification
            if (request.IsEnabled)
            {
                var medication = await _unitOfWork.Repository<Medication>().GetByIdAsync(reminder.MedicationId);
                if (medication != null)
                {
                    var scheduledTime = DateTime.Today.Add(reminder.ReminderTime);
                    if (scheduledTime < DateTime.Now)
                    {
                        scheduledTime = scheduledTime.AddDays(1);
                    }

                    var notificationId = await _notificationService.ScheduleNotificationAsync(
                        medication.Id,
                        scheduledTime,
                        $"Time to take {medication.Name}",
                        $"Take {medication.Dosage} {medication.Unit}",
                        reminder.VoiceRecording?.FilePath
                    );

                    reminder.NotificationId = notificationId;
                    await _unitOfWork.Repository<Reminder>().UpdateAsync(reminder);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            else if (!string.IsNullOrEmpty(reminder.NotificationId))
            {
                await _notificationService.CancelNotificationAsync(reminder.NotificationId);
            }

            return Ok(new { message = $"Reminder {(request.IsEnabled ? "enabled" : "disabled")} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling reminder {Id}", id);
            return StatusCode(500, new { message = "An error occurred while toggling reminder" });
        }
    }

    /// <summary>
    /// Calculate reminder times for a frequency
    /// </summary>
    [HttpPost("calculate")]
    public IActionResult CalculateReminderTimes([FromBody] CalculateRemindersRequest request)
    {
        try
        {
            var times = _schedulingService.CalculateReminderTimes(request.TimesPerDay);
            return Ok(new { timesPerDay = request.TimesPerDay, reminderTimes = times });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating reminder times");
            return StatusCode(500, new { message = "An error occurred while calculating reminder times" });
        }
    }

    /// <summary>
    /// Calculate custom reminder times from natural language
    /// </summary>
    [HttpPost("calculate-custom")]
    public IActionResult CalculateCustomReminderTimes([FromBody] CalculateCustomRemindersRequest request)
    {
        try
        {
            var times = _schedulingService.CalculateCustomReminderTimes(request.Frequency);
            return Ok(new { frequency = request.Frequency, reminderTimes = times });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating custom reminder times");
            return StatusCode(500, new { message = "An error occurred while calculating reminder times" });
        }
    }

    /// <summary>
    /// Delete a reminder
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReminder(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var reminder = await _unitOfWork.Repository<Reminder>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reminder == null)
            {
                return NotFound(new { message = "Reminder not found" });
            }

            // Verify ownership through medication
            if (reminder.Medication.UserId != userId)
            {
                return Forbid();
            }

            // Cancel notification if exists
            if (!string.IsNullOrEmpty(reminder.NotificationId))
            {
                await _notificationService.CancelNotificationAsync(reminder.NotificationId);
            }

            await _unitOfWork.Repository<Reminder>().DeleteAsync(reminder);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Reminder deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reminder {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting reminder" });
        }
    }

    // Helper method to get current user ID from JWT token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}

public record UpdateReminderTimeRequest(TimeSpan NewTime);
public record ToggleReminderRequest(bool IsEnabled);
public record CalculateRemindersRequest(int TimesPerDay);
public record CalculateCustomRemindersRequest(string Frequency);

