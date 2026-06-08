namespace Sisonke.Web.Services.Dto;

public class ClaimDocumentChecklistItemDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsSubmitted { get; set; }
    public string Status { get; set; } = "Missing";
    public string? FileName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? Notes { get; set; }
}
