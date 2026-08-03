using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class FeedbackService
{
    public FeedbackMessage Success(string message, string title = "Tudo certo") =>
        new(FeedbackType.Success, title, message, FeedbackPresentation.Toast);

    public FeedbackMessage Information(string message, string title = "Informação") =>
        new(FeedbackType.Information, title, message, FeedbackPresentation.Toast);

    public FeedbackMessage ForFailure(string message, bool databaseUnavailable = false) => databaseUnavailable
        ? new(FeedbackType.DatabaseUnavailable, "Serviço temporariamente indisponível", message, FeedbackPresentation.Modal)
        : new(FeedbackType.Error, "Não foi possível concluir", message, FeedbackPresentation.Modal);

    public FeedbackMessage Confirmation(string title, string message, FeedbackAction action) =>
        new(FeedbackType.Confirmation, title, message, FeedbackPresentation.Modal, action);
}
