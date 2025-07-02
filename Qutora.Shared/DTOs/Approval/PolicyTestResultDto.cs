namespace Qutora.Shared.DTOs.Approval;

public class PolicyTestResultDto
{
    public bool RequiresApproval { get; set; }

    public bool IsApprovalRequired { get; set; }

    public string? Reason { get; set; }

    public List<string> MatchedRules { get; set; } = new();

    public int RequiredApprovalCount { get; set; }

    public int ApprovalTimeoutHours { get; set; }

    public List<string> AssignedApprovers { get; set; } = new();

    public List<string>? RequiredApproverRoles { get; set; }
}