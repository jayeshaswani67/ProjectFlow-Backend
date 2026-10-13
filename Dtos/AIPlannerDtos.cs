namespace ProjectFlow.Api.DTOs;

public class GeneratePlannerRequest
{
public string ProjectName { get; set; } = string.Empty;

public string Description { get; set; } = string.Empty;

public DateTime Deadline { get; set; }

public int TeamSize { get; set; } = 1;

public string Priority { get; set; } = "Medium";


}

public class PlannerResponse
{
public string ProjectName { get; set; } = string.Empty;
public string Summary { get; set; } = string.Empty;

public string DeliveryAssessment { get; set; } = string.Empty;

public DateTime StartDate { get; set; }

public DateTime DeliveryDate { get; set; }

public int TotalDays { get; set; }

public int EstimatedWorkDays { get; set; }

public List<PlannerMilestone> Milestones { get; set; } = new();

public List<PlannerRisk> Risks { get; set; } = new();

public List<string> Recommendations { get; set; } = new();


}

public class PlannerMilestone
{
public string Name { get; set; } = string.Empty;


public string Description { get; set; } = string.Empty;

public DateTime DueDate { get; set; }

public List<PlannerTask> Tasks { get; set; } = new();


}

public class PlannerTask
{
public string Title { get; set; } = string.Empty;


public string Description { get; set; } = string.Empty;

public string Phase { get; set; } = string.Empty;

public string Priority { get; set; } = "Medium";

public int EstimatedDays { get; set; }

public DateTime StartDate { get; set; }

public DateTime DueDate { get; set; }

public List<string> DependsOn { get; set; } = new();


}

public class PlannerRisk
{
public string Risk { get; set; } = string.Empty;


public string Severity { get; set; } = "Medium";

public string Mitigation { get; set; } = string.Empty;


}
