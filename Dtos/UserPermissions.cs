
namespace ProjectFlow.Api.DTOs;

public class UserPermissions
{
    
    // DASHBOARD
    

    public bool ViewDashboard { get; set; }


    
    // PROJECTS
    

    public bool ViewProjects { get; set; }

    public bool CreateProject { get; set; }

    public bool EditProject { get; set; }

    public bool DeleteProject { get; set; }


    
    // TASKS
    

    public bool ViewTasks { get; set; }

    public bool CreateTask { get; set; }

    public bool EditTask { get; set; }

    public bool DeleteTask { get; set; }

    public bool AssignTask { get; set; }


    
    // TEAM
    

    public bool ViewTeam { get; set; }

    public bool ManageTeam { get; set; }


    
    // ANALYTICS
    public bool ViewAnalytics { get; set; }


    
    // AI
    

    public bool UseAIPlanner { get; set; }


    
    // USER MANAGEMENT
    

    public bool ManageUsers { get; set; }

    public bool ManageRoles { get; set; }


    
    // SYSTEM
    

    public bool SystemSettings { get; set; }

    public bool SubmitWork { get; set; }

    public bool ReviewWork { get; set; }
}
