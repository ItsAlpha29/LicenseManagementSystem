namespace LicenseManagement.API.DTOs;

public class AiChatMessageDto
{
    public string Role { get; set; } = "user"; // system, user, assistant, tool
    public string Content { get; set; } = string.Empty;
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
}

public class AiChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<AiChatMessageDto>? ConversationHistory { get; set; }
}

public class AiProposedActionDto
{
    public string ActionType { get; set; } = string.Empty; // ASSIGN_LICENSE, REVOKE_LICENSE, RENEW_LICENSE
    public string ActionTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool RequiresConfirmation { get; set; } = true;
    public string ConfirmationToken { get; set; } = string.Empty;
}

public class AiChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public List<string> SourcesOrToolsUsed { get; set; } = new();
    public AiProposedActionDto? ProposedAction { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class AiActionConfirmRequestDto
{
    public string ConfirmationToken { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class AiActionConfirmResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? ResultData { get; set; }
}

// ----------------------------------------------------
// Software Recommendation DTOs
// ----------------------------------------------------

public class SoftwareRecommendationRequestDto
{
    public int? EmployeeId { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
}

public class RecommendedSoftwareItemDto
{
    public int LicenseId { get; set; }
    public string SoftwareName { get; set; } = string.Empty;
    public string LicenseCode { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public int AvailableSeats { get; set; }
    public int TotalSeats { get; set; }
    public decimal AnnualCost { get; set; }
    public int MatchScore { get; set; } // 0 to 100
    public string Priority { get; set; } = "HIGH"; // HIGH, MEDIUM, OPTIONAL
    public string RecommendationReason { get; set; } = string.Empty;
    public bool IsCurrentlyAssigned { get; set; }
}

public class SoftwareRecommendationResultDto
{
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<RecommendedSoftwareItemDto> Recommendations { get; set; } = new();
}

// ----------------------------------------------------
// Duplicate & Redundancy Detection DTOs
// ----------------------------------------------------

public class DuplicateDetectionRequestDto
{
    public string SoftwareName { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string? LicenseType { get; set; }
    public string? LicenseReference { get; set; }
}

public class DuplicateMatchItemDto
{
    public int ExistingLicenseId { get; set; }
    public string ExistingSoftwareName { get; set; } = string.Empty;
    public string LicenseCode { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public string Status { get; set; } = string.Empty;
    public int SimilarityPercentage { get; set; } // 0 to 100
    public string MatchLevel { get; set; } = "HIGH"; // EXACT, HIGH, MODERATE, LOW
    public string Explanation { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty; // e.g. "Add seats to existing license", "Consolidate contract"
}

public class DuplicateDetectionResultDto
{
    public bool HasDuplicates { get; set; }
    public int HighestSimilarityPercentage { get; set; }
    public string Assessment { get; set; } = string.Empty;
    public List<DuplicateMatchItemDto> Matches { get; set; } = new();
}

public class RedundancyGroupDto
{
    public string CategoryOrFunction { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TotalAnnualSpend { get; set; }
    public decimal PotentialSavings { get; set; }
    public string ConsolidationAdvice { get; set; } = string.Empty;
    public List<DuplicateMatchItemDto> OverlappingLicenses { get; set; } = new();
}

public class PortfolioRedundancyScanResultDto
{
    public int TotalLicensesScanned { get; set; }
    public int RedundantGroupsFound { get; set; }
    public decimal EstimatedAnnualSavings { get; set; }
    public List<RedundancyGroupDto> RedundancyGroups { get; set; } = new();
}
