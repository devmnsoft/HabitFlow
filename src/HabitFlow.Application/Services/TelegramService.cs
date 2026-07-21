using System.Net.Http.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class TelegramService(HttpClient http, IConfiguration configuration, ILogger<TelegramService> logger)
{
    public async Task<Result> SendAsync(AuditSeverity severity, string text, CancellationToken ct = default)
    {
        try
        {
            if (!configuration.GetValue<bool>("Telegram:Enabled")) return Result.Success();
            var token = configuration["Telegram:BotToken"];
            var chat = configuration["Telegram:AdminChatId"];
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chat)) return Result.Failure("telegram.config", "Telegram não configurado.");
            var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/sendMessage", new { chat_id = chat, text = $"[{severity}] {text}" }, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure("telegram.send", "Falha ao enviar Telegram.");
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao enviar alerta Telegram"); return Result.Failure("telegram.error", "Falha ao enviar Telegram."); }
    }
}
