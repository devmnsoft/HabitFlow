using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record SupportContact(string CompanyName,string Email,string? WhatsAppUrl,string ButtonText,string BusinessHours);
public static class SupportSla
{
    public static int Hours(string priority)=>priority switch{"Low"=>72,"High"=>24,"Critical"=>8,_=>48};
    public static DateTime Calculate(DateTime openedUtc,string priority)
    {
        var remaining=Hours(priority); var cursor=DateTime.SpecifyKind(openedUtc,DateTimeKind.Utc);
        while(remaining>0){cursor=cursor.AddHours(1);if(cursor.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)remaining--;}
        return cursor;
    }
}
public sealed class SupportCenterService(IAssistanceRepository repository,WhatsAppValidator validator,ProtocolGenerator protocols,AssistantSafetyPolicy safety,ILogger<SupportCenterService> logger)
{
    public async Task<SupportContact> ContactAsync(CancellationToken ct){var s=await repository.GetSupportSettingsAsync(ct); string? url=null; if(s.IsActive&&!string.IsNullOrWhiteSpace(s.WhatsAppPhone)&&validator.Validate(new(true,s.WhatsAppPhone,s.DefaultMessage,s.ButtonText)).IsSuccess){var digits=new string(s.WhatsAppPhone.Where(char.IsDigit).ToArray());url=$"https://wa.me/{digits}?text={Uri.EscapeDataString(s.DefaultMessage)}";} return new(s.CompanyName,s.SupportEmail,url,s.ButtonText,s.BusinessHours);}
    public Task<IReadOnlyList<SupportTicketDetail>> ListAsync(Guid clientId,Guid userId,bool admin,CancellationToken ct)=>repository.ListTicketsAsync(clientId,userId,admin,ct);
    public async Task<SupportTicketDetail?> GetAsync(Guid clientId,Guid userId,Guid id,bool admin,CancellationToken ct)=>await repository.GetTicketAsync(clientId,userId,id,admin,ct);
    public async Task<Guid> CreateAsync(Guid clientId,Guid userId,string category,string priority,string subject,string description,string route,string browser,string viewport,string plan,string correlationId,CancellationToken ct){var id=Guid.NewGuid();var now=DateTime.UtcNow;priority=AllowedPriority(priority);var safe=$"route={Clean(route,120)}; browser={Clean(browser,160)}; viewport={Clean(viewport,30)}; app=6.19.3; correlation={Clean(correlationId,80)}; plan={Clean(plan,30)}; at={now:O}";await repository.CreateTicketAsync(new(id,clientId,userId,protocols.Generate("SUP"),AllowedCategory(category),priority,"Open",Clean(subject,160),safety.Sanitize(description),safe,null,SupportSla.Calculate(now,priority),now,now,null),ct);logger.LogInformation(ApplicationEvents.SupportTicketCreated,"support.ticket_created CorrelationId={CorrelationId} ClientId={ClientId} UserId={UserId} TicketId={TicketId} Category={Category} Priority={Priority}",correlationId,clientId,userId,id,category,priority);return id;}
    public async Task<bool> ReplyAsync(Guid clientId,Guid userId,Guid id,bool admin,string message,bool isInternal,CancellationToken ct){if(string.IsNullOrWhiteSpace(message)||isInternal&&!admin)return false;var ticket=await repository.GetTicketAsync(clientId,userId,id,admin,ct);if(ticket is null||ticket.Status is "Closed" or "Cancelled")return false;await repository.AddTicketMessageAsync(new(Guid.NewGuid(),clientId,id,userId,admin,isInternal,safety.Sanitize(message),DateTime.UtcNow),ct);logger.LogInformation(isInternal?"support.internal_note_added":"support.message_added","ClientId={ClientId} TicketId={TicketId} IsStaff={IsStaff}",clientId,id,admin);return true;}
    public async Task<bool> CloseAsync(Guid clientId,Guid userId,Guid id,bool admin,CancellationToken ct){if(await repository.GetTicketAsync(clientId,userId,id,admin,ct) is null)return false;await repository.UpdateTicketStatusAsync(clientId,id,"Closed",DateTime.UtcNow,ct);logger.LogInformation(ApplicationEvents.SupportTicketClosed,"support.ticket.closed ClientId={ClientId} UserId={UserId} TicketId={TicketId} Result=closed",clientId,userId,id);return true;}
    public async Task<IReadOnlyList<SupportTicketMessage>> MessagesAsync(Guid clientId,Guid ticketId,bool staff,CancellationToken ct)=>(await repository.ListTicketMessagesAsync(clientId,ticketId,ct)).Where(x=>staff||!x.IsInternal).ToArray();
    private static string Clean(string? value,int max){var v=(value??string.Empty).Replace('\r',' ').Replace('\n',' ').Trim();return v.Length<=max?v:v[..max];}
    private static string AllowedCategory(string c)=>new[]{"Question","Error","Billing","Access","Configuration","Suggestion","Commercial"}.Contains(c)?c:"Question";
    private static string AllowedPriority(string p)=>new[]{"Low","Medium","High","Critical"}.Contains(p)?p:"Medium";
}
