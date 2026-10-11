# HabitFlow Navigation and Access Matrix

Last reviewed: 2026-10-10

This matrix keeps the product journey, navigation entry points, route ownership, and access rules aligned. The source of truth for rendered navigation remains `NavigationService.Definitions`; this document explains the intent behind the daily experience.

| Journey area | Main route | Navigation context | Audience | Plan or feature guard | Security rule |
| --- | --- | --- | --- | --- | --- |
| Public overview | `/` | Public | Visitors | None | Anonymous only content; no tenant data. |
| Habit library discovery | `/habit-library` | Public and Personal | Visitors and signed-in users | Public templates are visible; activation still checks plan limits. | Template activation revalidates tenant, ownership, and entitlement server-side. |
| Daily command center | `/my-day` | Personal | Signed-in users | None | User and client ids come from claims; no route-supplied tenant trust. |
| Today dashboard | `/dashboard` | Personal | Signed-in users | None | Lists only habits for the current user/client pair. |
| Guided habit creation | `/habits/create` | Personal | Signed-in users | Active habit limit via `active_habits_limit`. | POST requires antiforgery, authenticated identity, entitlement check, normalized schedule, planning validation, and audit event. |
| Habit editing | `/habits/{id}/edit` | Personal | Habit owner | Existing habit ownership. | Loader and saver require matching user/client ids; adaptive changes preserve completions and history. |
| Habit detail and calendar | `/habits/{id}` | Personal | Habit owner | None | Detail query scopes calendar, streak and timeline to owner/client. |
| Progress calendar | `/progress/calendar` | Personal | Signed-in users | None | Uses scoped progress rows and local timezone calculations. |
| Reminders | `/reminders` | Personal | Signed-in users | None | Reminder operations must resolve the current identity and never trust arbitrary user ids. |
| Reports | `/reports` | Personal | Signed-in users | `basic_reports` | Menu visibility and route access must both check feature entitlement. |
| Account and preferences | `/profile` | Account | Signed-in users | None | User preference changes are scoped to the signed-in user. |
| People management | `/account/people` | Account | Account admins | `Client.Users.Manage` permission | Tenant membership and capacity are checked before mutations. |
| Billing | `/billing` | Account | Billing admins | `Client.Billing.View` permission | Provider callbacks remain server-authenticated and audited. |
| Admin settings | `/admin/settings` | Platform | Tenant admins | Tenant admin permission set | Changes are audited and tenant-scoped. |
| Super admin | `/superadmin` | Platform | Super admins | Super admin role/permission | Isolated from tenant user flows and never exposed in personal navigation. |

Daily experience rules:

- Creation starts with identity, then tracking, calendar, and reminder so the user can decide what the habit means before scheduling it.
- Quantitative habits require quantity and unit together; binary habits do not send hidden quantitative fields.
- Start and end dates only define future scheduling. They do not delete existing completions.
- Retroactive adjustment defaults to seven days and is capped at thirty-one days by the application service.
- Navigation visibility is a convenience, not authorization. Controllers and use cases remain responsible for final permission, plan, and tenant checks.
