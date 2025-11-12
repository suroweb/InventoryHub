using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ServerApp.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, Guid tenantId);
    Task<AuthResult> LoginAsync(string email, string password);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public ApplicationUser? User { get; set; }
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly MasterDbContext _masterDbContext;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        MasterDbContext masterDbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _masterDbContext = masterDbContext;
    }

    public async Task<AuthResult> RegisterAsync(string email, string password, string firstName, string lastName, Guid tenantId)
    {
        // Verify tenant exists and is active
        var tenant = await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);

        if (tenant == null || !tenant.IsSubscriptionActive())
        {
            return new AuthResult
            {
                Success = false,
                Error = "Invalid tenant or subscription expired"
            };
        }

        // Check user limit
        var userCount = await _masterDbContext.Users.CountAsync(u => u.TenantId == tenantId);
        if (userCount >= tenant.MaxUsers)
        {
            return new AuthResult
            {
                Success = false,
                Error = $"User limit reached for this subscription tier ({tenant.MaxUsers} users)"
            };
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenantId,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return new AuthResult
            {
                Success = false,
                Error = string.Join(", ", result.Errors.Select(e => e.Description))
            };
        }

        var token = GenerateJwtToken(user);

        return new AuthResult
        {
            Success = true,
            Token = token,
            User = user
        };
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            return new AuthResult
            {
                Success = false,
                Error = "Invalid credentials or account inactive"
            };
        }

        // Check if tenant subscription is active
        var tenant = await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == user.TenantId);

        if (tenant == null || !tenant.IsSubscriptionActive())
        {
            return new AuthResult
            {
                Success = false,
                Error = "Tenant subscription expired or inactive"
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return new AuthResult
            {
                Success = false,
                Error = result.IsLockedOut ? "Account locked out" : "Invalid credentials"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var token = GenerateJwtToken(user);

        return new AuthResult
        {
            Success = true,
            Token = token,
            User = user
        };
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("TenantId", user.TenantId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(7);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "InventoryHub",
            audience: _configuration["Jwt:Audience"] ?? "InventoryHub",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
