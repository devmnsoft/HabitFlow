namespace HabitFlow.Web.Models;

public sealed record PrivacyConsentViewModel(string Key, string Title, string Description, bool Granted, DateTime? UpdatedAt);
public sealed record PrivacyRequestViewModel(Guid Id, string Protocol, string Type, string Status, DateTime CreatedAt);
public sealed record DataExportRequestViewModel(bool HasPendingRequest);
public sealed record DataDeletionRequestViewModel(bool HasPendingDeletion, bool HasPendingAnonymization);
public sealed record PrivacyActivityViewModel(string Title, string Detail, DateTime OccurredAt);
public sealed record AccountPrivacyViewModel(
    string UserName, string Email, DateTime AccountCreatedAt,
    IReadOnlyList<PrivacyConsentViewModel> Consents,
    IReadOnlyList<PrivacyRequestViewModel> Requests,
    IReadOnlyList<PrivacyActivityViewModel> Activity,
    DataExportRequestViewModel Export,
    DataDeletionRequestViewModel Deletion);
