using MongoDB.Driver;
using ProjectFlow.Api.Data;
using ProjectFlow.Api.DTOs;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Services;

public class WorkSubmissionService
{
    private readonly MongoDbContext _db;

    public WorkSubmissionService(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<List<WorkSubmissionResponse>> GetEmployeeSubmissionsAsync(
        string employeeId)
    {
        var submissions = await _db.WorkSubmissions
            .Find(x => x.EmployeeId == employeeId)
            .SortByDescending(x => x.SubmittedAt)
            .ToListAsync();

        return submissions
            .Select(Map)
            .ToList();
    }

    public async Task<WorkSubmissionResponse?> GetByTaskAsync(
        string taskId,
        string employeeId)
    {
        var submission = await _db.WorkSubmissions
            .Find(x =>
                x.TaskId == taskId &&
                x.EmployeeId == employeeId)
            .SortByDescending(x => x.SubmittedAt)
            .FirstOrDefaultAsync();

        return submission == null
            ? null
            : Map(submission);
    }

    public async Task<WorkSubmissionResponse> CreateAsync(
        CreateWorkSubmissionRequest request,
        string employeeId)
    {
        if (string.IsNullOrWhiteSpace(request.TaskId))
            throw new ArgumentException("Task is required.");

        if (string.IsNullOrWhiteSpace(request.Notes))
            throw new ArgumentException("Work notes are required.");

        var submission = new WorkSubmission
        {
            Id = Guid.NewGuid().ToString(),

            TaskId = request.TaskId,

            EmployeeId = employeeId,

            Notes = request.Notes.Trim(),

            AttachmentUrl =
                string.IsNullOrWhiteSpace(request.AttachmentUrl)
                    ? null
                    : request.AttachmentUrl.Trim(),

            Status = "PendingReview",

            SubmittedAt = DateTime.UtcNow
        };

        await _db.WorkSubmissions.InsertOneAsync(
            submission);

        return Map(submission);
    }

    private static WorkSubmissionResponse Map(
        WorkSubmission submission)
    {
        return new WorkSubmissionResponse
        {
            Id = submission.Id,
            TaskId = submission.TaskId,
            EmployeeId = submission.EmployeeId,
            Notes = submission.Notes,
            AttachmentUrl = submission.AttachmentUrl,
            Status = submission.Status,
            ManagerFeedback = submission.ManagerFeedback,
            SubmittedAt = submission.SubmittedAt,
            ReviewedAt = submission.ReviewedAt
        };
    }
}