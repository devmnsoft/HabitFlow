using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Models;
public sealed record HelpIndexViewModel(IReadOnlyList<HelpArticle> Articles,IReadOnlyList<string> Categories,string? Query,string? Category,SupportContact Contact);
public sealed record HelpArticleViewModel(HelpArticle Article,IReadOnlyList<HelpArticle> Related,SupportContact Contact);
public sealed record SupportIndexViewModel(bool IsAuthenticated,IReadOnlyList<SupportTicketDetail> Tickets,SupportContact Contact);
public sealed record TicketDetailViewModel(SupportTicketDetail Ticket,IReadOnlyList<SupportTicketMessage> Messages,SupportContact Contact);
