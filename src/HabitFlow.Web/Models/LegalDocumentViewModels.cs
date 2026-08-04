using System.ComponentModel.DataAnnotations;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;

public sealed class LegalDocumentEditViewModel
{
    public Guid DocumentId { get; set; }
    public Guid VersionId { get; set; }
    [Required] public LegalDocumentType DocumentType { get; set; }
    [Required, StringLength(30)] public string Version { get; set; } = "1.0";
    [Required, StringLength(12)] public string Locale { get; set; } = "pt-BR";
    [Required, StringLength(180)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(1000)] public string Summary { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    [Required] public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
    public bool RequiresReacceptance { get; set; }

    public LegalVersionDraft ToDraft() => new(Version, Locale, Title, Summary, Content, DateTime.SpecifyKind(EffectiveAt, DateTimeKind.Utc), RequiresReacceptance);
}
