using MongoDB.Driver;
using ProjectFlow.Api.Configuration;
using ProjectFlow.Api.Models;


namespace ProjectFlow.Api.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(
        IMongoClient client,
        MongoDbSettings settings)
    {
        _database = client.GetDatabase(
            settings.DatabaseName
        );
    }

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<RefreshToken> RefreshTokens =>
        _database.GetCollection<RefreshToken>(
            "refreshTokens"
        );

    public IMongoCollection<Project> Projects =>
        _database.GetCollection<Project>(
            "projects"
        );

    public IMongoCollection<TaskItem> Tasks =>
        _database.GetCollection<TaskItem>(
            "tasks"

            
        );
        public IMongoCollection<TeamMember> TeamMembers =>
    _database.GetCollection<TeamMember>("teamMembers");
    public IMongoCollection<AppSettings> Settings =>
    _database.GetCollection<AppSettings>("Settings");
    public IMongoCollection<WorkSubmission> WorkSubmissions =>
    _database.GetCollection<WorkSubmission>("workSubmissions");

    
    

}