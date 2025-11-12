using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Services;

namespace ServerApp.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks")
            .RequireAuthorization();

        group.MapGet("/", GetWebhooksAsync)
            .WithName("GetWebhooks")
            .WithOpenApi();

        group.MapPost("/", CreateWebhookAsync)
            .WithName("CreateWebhook")
            .WithOpenApi();

        group.MapPut("/{webhookId:guid}/toggle", ToggleWebhookAsync)
            .WithName("ToggleWebhook")
            .WithOpenApi();

        group.MapDelete("/{webhookId:guid}", DeleteWebhookAsync)
            .WithName("DeleteWebhook")
            .WithOpenApi();

        group.MapPost("/test", TestWebhookAsync)
            .WithName("TestWebhook")
            .WithOpenApi();
    }

    private static async Task<IResult> GetWebhooksAsync(IWebhookService webhookService)
    {
        try
        {
            var webhooks = await webhookService.GetActiveWebhooksAsync();
            return Results.Ok(webhooks);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CreateWebhookAsync(
        [FromBody] CreateWebhookRequest request,
        IWebhookService webhookService)
    {
        try
        {
            var webhook = await webhookService.CreateWebhookAsync(
                request.Name,
                request.Url,
                request.Events,
                request.Secret
            );

            return Results.Created($"/api/v1/webhooks/{webhook.Id}", webhook);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ToggleWebhookAsync(
        Guid webhookId,
        [FromBody] ToggleWebhookRequest request,
        IWebhookService webhookService)
    {
        try
        {
            await webhookService.UpdateWebhookAsync(webhookId, request.IsActive);
            return Results.Ok(new { message = "Webhook updated" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteWebhookAsync(
        Guid webhookId,
        IWebhookService webhookService)
    {
        try
        {
            await webhookService.DeleteWebhookAsync(webhookId);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> TestWebhookAsync(
        [FromBody] TestWebhookRequest request,
        IWebhookService webhookService)
    {
        try
        {
            await webhookService.TriggerWebhookAsync(request.Event, new
            {
                test = true,
                message = "This is a test webhook",
                timestamp = DateTime.UtcNow
            });

            return Results.Ok(new { message = "Test webhook triggered" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public class CreateWebhookRequest
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required List<string> Events { get; set; }
    public string? Secret { get; set; }
}

public class ToggleWebhookRequest
{
    public bool IsActive { get; set; }
}

public class TestWebhookRequest
{
    public required string Event { get; set; }
}
