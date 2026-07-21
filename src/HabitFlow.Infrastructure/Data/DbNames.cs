namespace HabitFlow.Infrastructure.Data;

public static class DbNames
{
    public const string Schema = "habitflow";

    public static class Tables
    {
        public const string Users = "habitflow.users";
        public const string Habits = "habitflow.habits";
        public const string HabitCompletions = "habitflow.habit_completions";
        public const string SupportTickets = "habitflow.support_tickets";
        public const string SupportMessages = "habitflow.support_messages";
        public const string SystemAuditLogs = "habitflow.system_audit_logs";
        public const string AdminAuditLogs = "habitflow.admin_audit_logs";
        public const string SystemSettings = "habitflow.system_settings";
        public const string LgpdRequests = "habitflow.lgpd_requests";
        public const string BillingEvents = "habitflow.billing_events";
        public const string Notifications = "habitflow.notifications";
        public const string UserReports = "habitflow.user_reports";
    }
}
