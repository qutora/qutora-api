using System.ComponentModel.DataAnnotations;
using Qutora.Domain.Base;
using Qutora.Shared.Enums;

namespace Qutora.Domain.Entities;

public class EmailTemplate : BaseEntity
{
    /// <summary>
    /// Template type (enum)
    /// </summary>
    [Required]
    public EmailTemplateType TemplateType { get; set; }



    /// <summary>
    /// Template description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Email subject template
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body template (HTML)
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Available variables for this template (JSON array)
    /// </summary>
    [StringLength(1000)]
    public string? AvailableVariables { get; set; }

    /// <summary>
    /// Is this template active?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Is this a system template (cannot be deleted)
    /// </summary>
    public bool IsSystem { get; set; } = false;
} 