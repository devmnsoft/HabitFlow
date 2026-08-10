using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Services;

public sealed class UserFeedbackService(ApplicationFeedbackService feedback)
{
    public void Success(Controller controller, string message) => feedback.SetSuccess(controller, "Solicitação registrada", message);
    public void Error(Controller controller, string message) => feedback.SetError(controller, "Não foi possível concluir", message);
}
