using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class SettingsService
{
    private const string SettingsId = "system";
    private readonly MongoDbContext _db;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        MongoDbContext db,
        ILogger<SettingsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SettingsResponse> GetAsync()
    {
        try
        {
            _logger.LogInformation("Loading application settings with id {SettingsId}", SettingsId);

            var settings = await _db.Settings
                .Find(x => x.Id == SettingsId)
                .FirstOrDefaultAsync();

            if (settings is null)
            {
                _logger.LogWarning("No application settings found for id {SettingsId}; returning defaults", SettingsId);
                settings = new AppSettings
                {
                    Id = SettingsId,
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return ToResponse(settings);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to load application settings with id {SettingsId}", SettingsId);
            throw;
        }
    }

    public async Task<SettingsResponse> UpdateAsync(
        UpdateSettingsRequest request)
    {
        try
        {
            _logger.LogInformation("Updating application settings with id {SettingsId}", SettingsId);

            var settings = new AppSettings
            {
                Id = SettingsId,
                ApplicationName = request.ApplicationName,
                CompanyName = request.CompanyName,
                Timezone = request.Timezone,
                DefaultProjectStatus = request.DefaultProjectStatus,
                DefaultTaskPriority = request.DefaultTaskPriority,
                AllowEmployeeCreateProjects = request.AllowEmployeeCreateProjects,
                AllowEmployeeCreateTasks = request.AllowEmployeeCreateTasks,
                RequireTaskAssignment = request.RequireTaskAssignment,
                EmailNotifications = request.EmailNotifications,
                TaskNotifications = request.TaskNotifications,
                ProjectNotifications = request.ProjectNotifications,
                UpdatedAt = DateTime.UtcNow
            };

            await _db.Settings.ReplaceOneAsync(
                x => x.Id == SettingsId,
                settings,
                new ReplaceOptions { IsUpsert = true });

            _logger.LogInformation("Application settings updated successfully with id {SettingsId}", SettingsId);
            return ToResponse(settings);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update application settings with id {SettingsId}", SettingsId);
            throw;
        }
    }

    private static SettingsResponse ToResponse(AppSettings settings)
    {
        return new SettingsResponse(
            settings.ApplicationName,
            settings.CompanyName,
            settings.Timezone,
            settings.DefaultProjectStatus,
            settings.DefaultTaskPriority,
            settings.AllowEmployeeCreateProjects,
            settings.AllowEmployeeCreateTasks,
            settings.RequireTaskAssignment,
            settings.EmailNotifications,
            settings.TaskNotifications,
            settings.ProjectNotifications,
            settings.UpdatedAt);
    }
}
