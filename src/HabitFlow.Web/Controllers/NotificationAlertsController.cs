using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Controllers;

[Authorize]
[Route("notifications/alerts")]
public sealed class NotificationAlertsController(
    ProductActivationService activation,
    PushNotificationPreferenceService preferences,
    IAssistanceRepository assistance,
    CurrentUserContext currentUser,
    IOptions<EmailOptions> email) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        var alerts = await EvaluateAsync(ct);
        var settings = await assistance.GetSupportSettingsAsync(ct);
        ViewData["Channels"] = alerts[0].ChannelNote;
        ViewData["ManualContact"] = string.IsNullOrWhiteSpace(settings.WhatsAppPhone) ? "comercial@mnsoft.com.br" : settings.SupportEmail;
        return View("~/Views/Notifications/Alerts.cshtml", alerts);
    }

    [HttpPost("{code}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string code, CancellationToken ct)
    {
        if (!currentUser.ClientId.HasValue) return Forbid();
        var alert = (await EvaluateAsync(ct)).FirstOrDefault(item => item.Code == code);
        if (alert is null) return NotFound();
        var result = await activation.CreateInAppAlertAsync(currentUser.ClientId.Value, currentUser.UserId, alert, ct);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Aviso criado no aplicativo." : result.Error.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyList<AlertEvaluation>> EvaluateAsync(CancellationToken ct)
    {
        var preference = await preferences.GetAsync(currentUser.ClientId!.Value, currentUser.UserId, ct);
        var settings = await assistance.GetSupportSettingsAsync(ct);
        var report = await activation.ReportAsync(new HomologationQuery(null, null, null, null, null, currentUser.ClientId), ct);
        var row = report.Rows.FirstOrDefault();
        bool? limit = row is null ? null : row.Plan.Equals("Free", StringComparison.OrdinalIgnoreCase) ? row.Habits >= AppConstants.FreePlanHabitLimit : null;
        var signals = new AlertSignals(
            HabitDue: null,
            GoalNear: null,
            Inactive: row is null ? null : !row.Active15Days,
            WeeklyDue: null,
            PlanLimitReached: limit,
            SubscriptionAlert: row is null ? null : CustomerSuccessBoard.PaymentPending(row) || CustomerSuccessBoard.Overdue(row),
            LowAdhesion: currentUser.IsAdmin ? row is null ? null : row.Habits > 0 && row.ActiveUsers == 0 : false,
            ChurnRisk: currentUser.IsSuperAdmin ? row is null ? null : CustomerSuccessBoard.Score(row).Health == "Risco" : false,
            preference.HabitReminders,
            preference.WeeklySummary,
            preference.InternalEnabled,
            currentUser.IsAdmin,
            currentUser.IsSuperAdmin,
            email.Value.Enabled && !string.IsNullOrWhiteSpace(email.Value.Smtp.Password),
            !string.IsNullOrWhiteSpace(settings.WhatsAppPhone));
        return NotificationDispatchPolicy.Evaluate(signals);
    }
}
