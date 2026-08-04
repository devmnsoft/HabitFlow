using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public sealed class SuperAdminLegalDocumentsController(LegalDocumentQueryService queries, LegalDocumentService documents,
    LegalPublicationService publication, ILogger<SuperAdminLegalDocumentsController> logger) : Controller
{
    [HttpGet("/superadmin/legal-documents")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await queries.ListAsync(ct));

    [HttpGet("/superadmin/legal-documents/create")]
    public IActionResult Create() => View(new LegalDocumentEditViewModel());

    [ValidateAntiForgeryToken, HttpPost("/superadmin/legal-documents/create")]
    public async Task<IActionResult> Create(LegalDocumentEditViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var version = await documents.CreateAsync(model.DocumentType, model.ToDraft(), this.CurrentUserId(), ct);
            logger.LogInformation("Legal draft created {DocumentId} {VersionId}", version.DocumentId, version.Id);
            TempData["Success"] = "Rascunho jurídico criado com segurança.";
            return RedirectToAction(nameof(Versions), new { id = version.DocumentId });
        }
        catch (ArgumentException ex) { ModelState.AddModelError(string.Empty, ex.Message); return View(model); }
    }

    [HttpGet("/superadmin/legal-documents/{id:guid}/versions")]
    public async Task<IActionResult> Versions(Guid id, CancellationToken ct) { ViewBag.DocumentId = id; return View(await queries.VersionsAsync(id, ct)); }

    [HttpGet("/superadmin/legal-documents/{id:guid}/edit/{versionId:guid}")]
    public async Task<IActionResult> Edit(Guid id, Guid versionId, CancellationToken ct)
    {
        var item = await queries.VersionAsync(id, versionId, ct);
        if (item is null) return NotFound();
        if (item.Status != LegalDocumentStatus.Draft) return Conflict("Versões publicadas são imutáveis.");
        return View(new LegalDocumentEditViewModel { DocumentId=id, VersionId=versionId, Version=item.Version, Locale=item.Locale,
            Title=item.Title, Summary=item.Summary, Content=item.SanitizedContent, EffectiveAt=item.EffectiveAt, RequiresReacceptance=item.RequiresReacceptance });
    }

    [ValidateAntiForgeryToken, HttpPost("/superadmin/legal-documents/{id:guid}/edit/{versionId:guid}")]
    public async Task<IActionResult> Edit(Guid id, Guid versionId, LegalDocumentEditViewModel model, CancellationToken ct)
    {
        if (id != model.DocumentId || versionId != model.VersionId) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        try { await documents.UpdateDraftAsync(id, versionId, model.ToDraft(), this.CurrentUserId(), ct); TempData["Success"] = "Rascunho atualizado."; return RedirectToAction(nameof(Versions), new { id }); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpGet("/superadmin/legal-documents/{id:guid}/preview/{versionId:guid}")]
    public async Task<IActionResult> Preview(Guid id, Guid versionId, CancellationToken ct) =>
        await queries.VersionAsync(id, versionId, ct) is { } item ? View(item) : NotFound();

    [ValidateAntiForgeryToken, HttpPost("/superadmin/legal-documents/{id:guid}/publish/{versionId:guid}")]
    public async Task<IActionResult> Publish(Guid id, Guid versionId, CancellationToken ct)
    { await publication.PublishAsync(id, versionId, ct); logger.LogInformation("Legal version published {DocumentId} {VersionId}", id, versionId); return RedirectToAction(nameof(Versions), new { id }); }

    [ValidateAntiForgeryToken, HttpPost("/superadmin/legal-documents/{id:guid}/archive/{versionId:guid}")]
    public async Task<IActionResult> Archive(Guid id, Guid versionId, CancellationToken ct)
    { await publication.ArchiveAsync(id, versionId, ct); logger.LogInformation("Legal version archived {DocumentId} {VersionId}", id, versionId); return RedirectToAction(nameof(Versions), new { id }); }
}
