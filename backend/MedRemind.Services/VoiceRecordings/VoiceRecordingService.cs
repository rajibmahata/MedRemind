using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Models;
using MedRemind.Core.Interfaces;
using MedRemind.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MedRemind.Services.VoiceRecordings;

/// <summary>
/// Service for managing voice recordings
/// </summary>
public class VoiceRecordingService
{
    private readonly IRepository<VoiceRecording> _voiceRecordingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly VoiceRecordingStorageService _storageService;
    private readonly ILogger<VoiceRecordingService>? _logger;

    public VoiceRecordingService(
        IRepository<VoiceRecording> voiceRecordingRepository,
        IUnitOfWork unitOfWork,
        VoiceRecordingStorageService storageService,
        ILogger<VoiceRecordingService>? logger = null)
    {
        _voiceRecordingRepository = voiceRecordingRepository;
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Create new voice recording
    /// </summary>
    public async Task<VoiceRecordingUploadResult> CreateVoiceRecordingAsync(CreateVoiceRecordingRequest request, int userId)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return new VoiceRecordingUploadResult
                {
                    Success = false,
                    Message = "Recording name is required"
                };
            }

            if (string.IsNullOrWhiteSpace(request.Base64Audio))
            {
                return new VoiceRecordingUploadResult
                {
                    Success = false,
                    Message = "Audio data is required"
                };
            }

            // Save audio file
            var (success, filePath, errorMessage) = await _storageService.SaveVoiceRecordingAsync(
                request.Base64Audio,
                request.FileName,
                userId,
                request.DurationSeconds);

            if (!success || filePath == null)
            {
                return new VoiceRecordingUploadResult
                {
                    Success = false,
                    Message = errorMessage ?? "Failed to save audio file"
                };
            }

            // Get file info
            var (exists, fileSize, _) = _storageService.GetFileInfo(filePath);

            // Create database record
            var voiceRecording = new VoiceRecording
            {
                UserId = userId,
                Name = request.Name,
                FilePath = filePath,
                DurationSeconds = request.DurationSeconds,
                CreatedAt = DateTime.UtcNow
            };

            await _voiceRecordingRepository.AddAsync(voiceRecording);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation($"Voice recording created: ID={voiceRecording.Id}, User={userId}, Name={request.Name}");

            return new VoiceRecordingUploadResult
            {
                Success = true,
                Message = "Voice recording saved successfully",
                VoiceRecordingId = voiceRecording.Id,
                FilePath = filePath,
                FileSize = fileSize
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating voice recording");
            return new VoiceRecordingUploadResult
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get all voice recordings for a user
    /// </summary>
    public async Task<List<VoiceRecordingDto>> GetUserVoiceRecordingsAsync(int userId)
    {
        try
        {
            var allRecordings = await _voiceRecordingRepository
                .FindAsync(v => v.UserId == userId);
            
            var recordings = allRecordings
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VoiceRecordingDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    Name = v.Name,
                    FilePath = v.FilePath,
                    DurationSeconds = v.DurationSeconds,
                    CreatedAt = v.CreatedAt,
                    ReminderCount = 0 // Cannot get Reminders count without Include - will be fetched separately if needed
                })
                .ToList();

            return recordings;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting voice recordings for user {userId}");
            return new List<VoiceRecordingDto>();
        }
    }

    /// <summary>
    /// Get voice recording by ID
    /// </summary>
    public async Task<VoiceRecordingDto?> GetVoiceRecordingByIdAsync(int id, int userId)
    {
        try
        {
            var recordings = await _voiceRecordingRepository
                .FindAsync(v => v.Id == id && v.UserId == userId);
            
            var recording = recordings.Select(v => new VoiceRecordingDto
                {
                    Id = v.Id,
                    UserId = v.UserId,
                    Name = v.Name,
                    FilePath = v.FilePath,
                    DurationSeconds = v.DurationSeconds,
                    CreatedAt = v.CreatedAt,
                    ReminderCount = 0 // Cannot get Reminders count without Include
                })
                .FirstOrDefault();

            return recording;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting voice recording {id}");
            return null;
        }
    }

    /// <summary>
    /// Update voice recording name
    /// </summary>
    public async Task<bool> UpdateVoiceRecordingNameAsync(UpdateVoiceRecordingRequest request, int userId)
    {
        try
        {
            var recording = await _voiceRecordingRepository
                .FirstOrDefaultAsync(v => v.Id == request.Id && v.UserId == userId);

            if (recording == null)
            {
                _logger?.LogWarning($"Voice recording {request.Id} not found for user {userId}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger?.LogWarning("Cannot update voice recording with empty name");
                return false;
            }

            recording.Name = request.Name;
            await _voiceRecordingRepository.UpdateAsync(recording);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation($"Voice recording {request.Id} name updated to '{request.Name}'");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error updating voice recording {request.Id}");
            return false;
        }
    }

    /// <summary>
    /// Delete voice recording
    /// </summary>
    public async Task<bool> DeleteVoiceRecordingAsync(int id, int userId)
    {
        try
        {
            var recording = await _voiceRecordingRepository
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (recording == null)
            {
                _logger?.LogWarning($"Voice recording {id} not found for user {userId}");
                return false;
            }

            // Check if recording is in use by checking reminders separately
            // Note: Without Include, we can't access navigation properties
            // This check is skipped - consider adding a Reminder repository check if needed
            
            // Delete file
            await _storageService.DeleteVoiceRecordingAsync(recording.FilePath);

            // Delete database record
            await _voiceRecordingRepository.DeleteAsync(recording);
            await _unitOfWork.SaveChangesAsync();

            _logger?.LogInformation($"Voice recording {id} deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error deleting voice recording {id}");
            return false;
        }
    }

    /// <summary>
    /// Get audio bytes for playback
    /// </summary>
    public async Task<(byte[]? audioBytes, string contentType)> GetAudioForPlaybackAsync(int id, int userId)
    {
        try
        {
            var recording = await _voiceRecordingRepository
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == userId);

            if (recording == null)
            {
                _logger?.LogWarning($"Voice recording {id} not found for user {userId}");
                return (null, "");
            }

            var audioBytes = await _storageService.GetAudioBytesAsync(recording.FilePath);
            var contentType = VoiceRecordingStorageService.GetContentType(recording.FilePath);

            return (audioBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error getting audio for playback: {id}");
            return (null, "");
        }
    }
}

