namespace HabitFlow.Web.Models;

public enum FeedbackType { Success, Information, Warning, Error, Confirmation, PlanLimit, ConcurrencyConflict, DatabaseUnavailable }
public enum FeedbackPresentation { Toast, Modal, Inline }

public sealed record FeedbackAction(string Label, string? Url = null);
public sealed record FeedbackMessage(FeedbackType Type, string Title, string Message, FeedbackPresentation Presentation, FeedbackAction? Action = null);
public record FeedbackModalViewModel(string Title, string Message, string PrimaryLabel = "Continuar", string SecondaryLabel = "Fechar", bool Critical = false);
public sealed record ConfirmationModalViewModel(string Title, string Message, string PrimaryLabel = "Confirmar", string SecondaryLabel = "Cancelar", bool Critical = false) : FeedbackModalViewModel(Title, Message, PrimaryLabel, SecondaryLabel, Critical);
public sealed record InformationModalViewModel(string Title, string Message, string PrimaryLabel = "Entendi") : FeedbackModalViewModel(Title, Message, PrimaryLabel);
public sealed record PlanLimitModalViewModel(string Title, string Message, string PrimaryLabel = "Ver planos") : FeedbackModalViewModel(Title, Message, PrimaryLabel);
public sealed record ConflictModalViewModel(string Title, string Message, string PrimaryLabel = "Atualizar") : FeedbackModalViewModel(Title, Message, PrimaryLabel);
