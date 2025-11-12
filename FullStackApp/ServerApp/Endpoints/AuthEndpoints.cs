using Microsoft.AspNetCore.Mvc;
using ServerApp.Services;
using Shared.DTOs;

namespace ServerApp.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .WithOpenApi();

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithOpenApi();
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        IAuthService authService)
    {
        try
        {
            var result = await authService.RegisterAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.TenantId);

            if (!result.Success)
            {
                return Results.BadRequest(new AuthResponse
                {
                    Success = false,
                    Error = result.Error
                });
            }

            return Results.Ok(new AuthResponse
            {
                Success = true,
                Token = result.Token,
                User = new UserDTO
                {
                    Id = result.User!.Id,
                    Email = result.User.Email!,
                    FullName = result.User.FullName,
                    TenantId = result.User.TenantId
                }
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new AuthResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        IAuthService authService)
    {
        try
        {
            var result = await authService.LoginAsync(request.Email, request.Password);

            if (!result.Success)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new AuthResponse
            {
                Success = true,
                Token = result.Token,
                User = new UserDTO
                {
                    Id = result.User!.Id,
                    Email = result.User.Email!,
                    FullName = result.User.FullName,
                    TenantId = result.User.TenantId
                }
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new AuthResponse
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}
