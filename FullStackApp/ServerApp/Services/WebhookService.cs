using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ServerApp.Services;

public interface IWebhookService
{
    Task TriggerWebhookAsync(string eventName, object payload);
    Task<List<Webhook>> GetActiveWebhooksAsync();
    Task<Webhook> CreateWebhookAsync(string name, string url, List<string> events, string? secret = null);
    Task UpdateWebhookAsync(Guid id, bool isActive);
    Task DeleteWebhookAsync(Guid id);
}

public class WebhookService : IWebhookService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        TenantDbContext context,
        ITenantService tenantService,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _tenantService = tenantService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task TriggerWebhookAsync(string eventName, object payload)
    {
        var webhooks = await _context.Webhooks
            .Where(w => w.IsActive && w.Events.Contains(eventName))
            .ToListAsync();

        foreach (var webhook in webhooks)
        {
            _ = Task.Run(async () => await ExecuteWebhookAsync(webhook, eventName, payload));
        }
    }

    private async Task ExecuteWebhookAsync(Webhook webhook, string eventName, object payload)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var startTime = DateTime.UtcNow;
        var attempt = 1;
        var maxRetries = webhook.RetryCount;

        while (attempt <= maxRetries)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(webhook.TimeoutSeconds);

                var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                // Add HMAC signature if secret is configured
                if (!string.IsNullOrEmpty(webhook.Secret))
                {
                    var signature = GenerateHMACSignature(payloadJson, webhook.Secret);
                    request.Headers.Add("X-Webhook-Signature", signature);
                }

                request.Headers.Add("X-Webhook-Event", eventName);
                request.Headers.Add("X-Webhook-Id", webhook.Id.ToString());

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Log webhook execution
                var log = new WebhookLog
                {
                    WebhookId = webhook.Id,
                    Event = eventName,
                    Payload = payloadJson,
                    StatusCode = (int)response.StatusCode,
                    Response = responseContent,
                    WasSuccessful = response.IsSuccessStatusCode,
                    AttemptNumber = attempt,
                    TriggeredAt = startTime,
                    DurationMs = duration
                };

                _context.WebhookLogs.Add(log);

                webhook.LastTriggeredAt = DateTime.UtcNow;
                webhook.LastStatus = response.IsSuccessStatusCode ? "Success" : $"Failed: {response.StatusCode}";

                await _context.SaveChangesAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Webhook {webhook.Name} executed successfully for event {eventName}");
                    break; // Success, no retry needed
                }
                else
                {
                    _logger.LogWarning($"Webhook {webhook.Name} failed with status {response.StatusCode}. Attempt {attempt}/{maxRetries}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Webhook {webhook.Name} failed with exception. Attempt {attempt}/{maxRetries}");

                var log = new WebhookLog
                {
                    WebhookId = webhook.Id,
                    Event = eventName,
                    Payload = payloadJson,
                    StatusCode = 0,
                    Response = ex.Message,
                    WasSuccessful = false,
                    AttemptNumber = attempt,
                    TriggeredAt = startTime,
                    DurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };

                _context.WebhookLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            attempt++;
            if (attempt <= maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
            }
        }
    }

    private string GenerateHMACSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    public async Task<List<Webhook>> GetActiveWebhooksAsync()
    {
        return await _context.Webhooks
            .Where(w => w.IsActive)
            .ToListAsync();
    }

    public async Task<Webhook> CreateWebhookAsync(string name, string url, List<string> events, string? secret = null)
    {
        var webhook = new Webhook
        {
            TenantId = _tenantService.GetTenantId(),
            Name = name,
            Url = url,
            Events = events,
            Secret = secret,
            IsActive = true,
            RetryCount = 3,
            TimeoutSeconds = 30
        };

        _context.Webhooks.Add(webhook);
        await _context.SaveChangesAsync();

        return webhook;
    }

    public async Task UpdateWebhookAsync(Guid id, bool isActive)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook != null)
        {
            webhook.IsActive = isActive;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteWebhookAsync(Guid id)
    {
        var webhook = await _context.Webhooks.FindAsync(id);
        if (webhook != null)
        {
            _context.Webhooks.Remove(webhook);
            await _context.SaveChangesAsync();
        }
    }
}
