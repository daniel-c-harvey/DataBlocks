using DataBlocks.DataAccess;
using Microsoft.AspNetCore.Identity;
using ScheMigrator.Migrations;

namespace DataBlocksTests.Models;

[ScheModel]
public class ApplicationUser : IdentityUser<long>, IModel
{
    public static DataSchema Schema { get; } = DataSchema.Create<ApplicationUser>("test-schema");

    [ScheKey("id")]
    public long ID { get; set; }
    
    [ScheData("user_name")]
    public override string? UserName { get; set; }
    
    [ScheData("normalized_user_name")]
    public override string? NormalizedUserName { get; set; }
    
    [ScheData("email")]
    public override string? Email { get; set; }
    
    [ScheData("normalized_email")]
    public override string? NormalizedEmail { get; set; }
    
    [ScheData("email_confirmed")]
    public override bool EmailConfirmed { get; set; }
    
    [ScheData("password_hash")]
    public override string? PasswordHash { get; set; }
    
    [ScheData("security_stamp")]
    public override string SecurityStamp { get; set; }
    
    [ScheData("concurrency_stamp")]
    public override string? ConcurrencyStamp { get; set; }
    
    [ScheData("phone_number")]
    public override string? PhoneNumber { get; set; }
    
    [ScheData("phone_number_confirmed")]
    public override bool PhoneNumberConfirmed { get; set; }
    
    [ScheData("two_factor_enabled")]
    public override bool TwoFactorEnabled { get; set; }
    
    [ScheData("lockout_end")]
    public override DateTimeOffset? LockoutEnd { get; set; }    
    
    [ScheData("lockout_enabled")]
    public override bool LockoutEnabled { get; set; }
    
    [ScheData("access_failed_count")]
    public override int AccessFailedCount { get; set; }

    [ScheData("deleted")]
    public bool Deleted { get; set; }
    
    [ScheData("create")]
    public DateTime Created { get; set; }
    
    [ScheData("modified")]
    public DateTime Modified { get; set; }
    
    public override long Id {
        get => ID;
        set => ID = value;
    }
}