namespace Qutora.Shared.Enums;

public enum ApprovalStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

public enum ApprovalAction
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4,
    Escalated = 5,
    Reassigned = 6
}

public enum ApprovalPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum ApprovalType
{
    SingleApproval = 1,
    MultipleApproval = 2,
    UnanimousApproval = 3,
    MajorityApproval = 4
}

public enum ApproverSelectionType
{
    AnyUser = 0,
    RoleBased = 1,
    UserBased = 2,
    DepartmentBased = 3,
    HierarchicalBased = 4,
    ContentBased = 5,
    SpecificUsers = 6,
    CreatedByUser = 7
}

public enum ShareType
{
    Public = 0,
    Private = 1,
    Protected = 2
}