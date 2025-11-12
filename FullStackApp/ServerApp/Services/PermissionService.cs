using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;

namespace ServerApp.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(string userId, Permission permission);
    Task<List<Permission>> GetUserPermissionsAsync(string userId);
    Task<Role> CreateRoleAsync(string name, string description, List<Permission> permissions);
    Task AssignRoleToUserAsync(string userId, Guid roleId, string assignedBy);
    Task RemoveRoleFromUserAsync(string userId, Guid roleId);
    Task<List<Role>> GetUserRolesAsync(string userId);
    Task InitializeDefaultRolesAsync();
}

public class PermissionService : IPermissionService
{
    private readonly TenantDbContext _context;
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;

    public PermissionService(
        TenantDbContext context,
        MasterDbContext masterContext,
        ITenantService tenantService)
    {
        _context = context;
        _masterContext = masterContext;
        _tenantService = tenantService;
    }

    public async Task<bool> HasPermissionAsync(string userId, Permission permission)
    {
        var permissions = await GetUserPermissionsAsync(userId);
        return permissions.Contains(permission);
    }

    public async Task<List<Permission>> GetUserPermissionsAsync(string userId)
    {
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role)
            .ToListAsync();

        var allPermissions = new HashSet<Permission>();

        foreach (var role in userRoles)
        {
            var rolePermissions = role.GetPermissions();
            foreach (var perm in rolePermissions)
            {
                allPermissions.Add(perm);
            }
        }

        return allPermissions.ToList();
    }

    public async Task<Role> CreateRoleAsync(string name, string description, List<Permission> permissions)
    {
        var role = new Role
        {
            TenantId = _tenantService.GetTenantId(),
            Name = name,
            Description = description,
            IsSystemRole = false
        };

        role.SetPermissions(permissions);

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }

    public async Task AssignRoleToUserAsync(string userId, Guid roleId, string assignedBy)
    {
        var exists = await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (!exists)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = assignedBy
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveRoleFromUserAsync(string userId, Guid roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Role>> GetUserRolesAsync(string userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role)
            .ToListAsync();
    }

    public async Task InitializeDefaultRolesAsync()
    {
        var tenantId = _tenantService.GetTenantId();

        // Check if roles already exist
        var rolesExist = await _context.Roles.AnyAsync();
        if (rolesExist) return;

        // Admin Role - Full access
        var adminRole = new Role
        {
            TenantId = tenantId,
            Name = "Administrator",
            Description = "Full system access",
            IsSystemRole = true
        };
        adminRole.SetPermissions(Enum.GetValues<Permission>().ToList());
        _context.Roles.Add(adminRole);

        // Manager Role
        var managerRole = new Role
        {
            TenantId = tenantId,
            Name = "Manager",
            Description = "Manage inventory, orders, and view analytics",
            IsSystemRole = true
        };
        managerRole.SetPermissions(new List<Permission>
        {
            Permission.ViewProducts, Permission.CreateProducts, Permission.EditProducts,
            Permission.ViewInventory, Permission.AdjustInventory, Permission.TransferStock,
            Permission.ViewOrders, Permission.CreateOrders, Permission.EditOrders,
            Permission.ViewSuppliers, Permission.ManageSuppliers,
            Permission.ViewAnalytics, Permission.ViewReports, Permission.CreateReports,
            Permission.ManageAlerts
        });
        _context.Roles.Add(managerRole);

        // Warehouse Staff Role
        var warehouseRole = new Role
        {
            TenantId = tenantId,
            Name = "Warehouse Staff",
            Description = "Manage stock levels and transfers",
            IsSystemRole = true
        };
        warehouseRole.SetPermissions(new List<Permission>
        {
            Permission.ViewProducts, Permission.ViewInventory,
            Permission.AdjustInventory, Permission.TransferStock,
            Permission.ViewOrders
        });
        _context.Roles.Add(warehouseRole);

        // Sales Role
        var salesRole = new Role
        {
            TenantId = tenantId,
            Name = "Sales",
            Description = "Create and manage orders",
            IsSystemRole = true
        };
        salesRole.SetPermissions(new List<Permission>
        {
            Permission.ViewProducts, Permission.ViewInventory,
            Permission.ViewOrders, Permission.CreateOrders, Permission.EditOrders,
            Permission.ViewAnalytics
        });
        _context.Roles.Add(salesRole);

        // Viewer Role - Read-only
        var viewerRole = new Role
        {
            TenantId = tenantId,
            Name = "Viewer",
            Description = "Read-only access",
            IsSystemRole = true
        };
        viewerRole.SetPermissions(new List<Permission>
        {
            Permission.ViewProducts, Permission.ViewInventory,
            Permission.ViewOrders, Permission.ViewSuppliers,
            Permission.ViewAnalytics, Permission.ViewReports
        });
        _context.Roles.Add(viewerRole);

        await _context.SaveChangesAsync();
    }
}
