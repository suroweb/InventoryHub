namespace ServerApp.Domain.Entities;

public enum Permission
{
    // Product Permissions
    ViewProducts = 1,
    CreateProducts = 2,
    EditProducts = 3,
    DeleteProducts = 4,

    // Inventory Permissions
    ViewInventory = 10,
    AdjustInventory = 11,
    TransferStock = 12,

    // Order Permissions
    ViewOrders = 20,
    CreateOrders = 21,
    EditOrders = 22,
    CancelOrders = 23,

    // Category Permissions
    ManageCategories = 30,

    // Supplier Permissions
    ViewSuppliers = 40,
    ManageSuppliers = 41,

    // Analytics Permissions
    ViewAnalytics = 50,
    ExportData = 51,

    // User Management
    ViewUsers = 60,
    ManageUsers = 61,
    AssignRoles = 62,

    // System Permissions
    ManageSettings = 70,
    ViewAuditLogs = 71,
    ManageIntegrations = 72,
    ManageLocations = 73,

    // Reporting
    ViewReports = 80,
    CreateReports = 81,
    ScheduleReports = 82,

    // Advanced Features
    ManageAutomation = 90,
    ManageAlerts = 91,
    AccessAPI = 92
}

public class Role : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } // Cannot be deleted

    // Serialized as JSON array
    public string PermissionsJson { get; set; } = "[]";

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public List<Permission> GetPermissions()
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<Permission>>(PermissionsJson) ?? new List<Permission>();
    }

    public void SetPermissions(List<Permission> permissions)
    {
        PermissionsJson = System.Text.Json.JsonSerializer.Serialize(permissions);
    }
}

public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
}
