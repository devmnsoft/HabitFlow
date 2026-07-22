using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Services;

public sealed class ApplicationFeedbackService
{
    public void SetSuccess(Controller controller, string title, string message) => SetFeedback(controller, "success", title, message);
    public void SetInfo(Controller controller, string title, string message) => SetFeedback(controller, "info", title, message);
    public void SetWarning(Controller controller, string title, string message) => SetFeedback(controller, "warning", title, message);
    public void SetError(Controller controller, string title, string message) => SetFeedback(controller, "error", title, message, modal: true);
    public void SetDatabaseError(Controller controller, string title, string message) => SetFeedback(controller, "database", title, message, modal: true);
    public void SetModal(Controller controller, string type, string title, string message) => SetFeedback(controller, type, title, message, modal: true);

    public void SetFeedback(Controller controller, string type, string title, string message) => SetFeedback(controller, type, title, message, modal: false);

    private static void SetFeedback(Controller controller, string type, string title, string message, bool modal)
    {
        controller.TempData["Feedback.Type"] = type;
        controller.TempData["Feedback.Title"] = title;
        controller.TempData["Feedback.Message"] = message;
        controller.TempData["Feedback.Modal"] = modal ? "true" : "false";
    }
}
