using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HabitFlow.Infrastructure;

public sealed class GmailSmtpEmailSender(IOptions<EmailOptions> options) : ITransactionalEmailSender
{
    public async Task SendAsync(TransactionalEmailMessage message, CancellationToken ct = default)
    {
        var config = options.Value;
        if (!config.Enabled) return;
        if (!config.Smtp.UseStartTls) throw new InvalidOperationException("STARTTLS é obrigatório.");
        if (string.IsNullOrWhiteSpace(config.Smtp.Password)) throw new InvalidOperationException("Email__Smtp__Password não configurada.");
        var content = JsonSerializer.Deserialize<TransactionalEmailContent>(message.PayloadJson) ?? throw new InvalidOperationException("Payload de e-mail inválido.");
        var mime = new MimeMessage { MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId() };
        mime.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        mime.ReplyTo.Add(MailboxAddress.Parse(config.ReplyToAddress));
        mime.To.Add(MailboxAddress.Parse(message.Recipient));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { TextBody = content.Text, HtmlBody = content.Html }.ToMessageBody();
        using var smtp = new SmtpClient { Timeout = config.Smtp.TimeoutSeconds * 1000 };
        await smtp.ConnectAsync(config.Smtp.Host, config.Smtp.Port, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(config.Smtp.Username, config.Smtp.Password, ct);
        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}

public sealed class TransactionalEmailProcessor(ITransactionalEmailOutboxRepository outbox,
    ITransactionalEmailSender sender, TimeProvider clock, ILogger<TransactionalEmailProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        foreach (var item in await outbox.ClaimBatchAsync(20, ct))
        {
            try { await sender.SendAsync(item, ct); await outbox.MarkSentAsync(item.Id, clock.GetUtcNow().UtcDateTime, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning("Transactional email {MessageId} failed on attempt {Attempt}", item.Id, item.Attempts + 1);
                await outbox.MarkFailedAsync(item.Id, ex.GetType().Name, clock.GetUtcNow().UtcDateTime.AddMinutes(Math.Pow(2, Math.Min(item.Attempts, 6))), 5, ct);
            }
        }
    }
}
