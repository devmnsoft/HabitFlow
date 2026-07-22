using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Services;

public sealed class ApplicationFeedbackService
{
    public void SetSuccess(Controller c, string title, string message) => Set(c, "success", title, message, false);
    public void SetInfo(Controller c, string title, string message) => Set(c, "info", title, message, false);
    public void SetWarning(Controller c, string title, string message) => Set(c, "warning", title, message, false);
    public void SetError(Controller c, string title, string message) => Set(c, "error", title, message, true);
    public void SetDatabaseError(Controller c, string title, string message) => Set(c, "database", title, message, true);
    public void SetModal(Controller c, string type, string title, string message) => Set(c, type, title, message, true);
    private static void Set(Controller c, string type, string title, string message, bool modal) { c.TempData["Feedback.Type"] = type; c.TempData["Feedback.Title"] = title; c.TempData["Feedback.Message"] = message; c.TempData["Feedback.Modal"] = modal ? "true" : "false"; }
}
