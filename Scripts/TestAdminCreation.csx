using Microsoft.AspNetCore.Identity;
using System.Data.SqlClient;

var email = "admin@test.com";
var password = "Admin@123";
var server = "localhost\\SQLEXPRESS";
var database = "MyStore";

Console.WriteLine("Testing Password Hasher...");
var hasher = new PasswordHasher<string>();
var hash = hasher.HashPassword(email, password);

Console.WriteLine($"? Password hashed successfully");
Console.WriteLine($"  Email: {email}");
Console.WriteLine($"  Hash Length: {hash.Length}");
Console.WriteLine($"  Hash Preview: {hash.Substring(0, Math.Min(50, hash.Length))}...");
Console.WriteLine();

// Escape for SQL
var escapedEmail = email.Replace("'", "''");
var escapedHash = hash.Replace("'", "''");

var sql = $@"
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- Check if user exists
IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = '{escapedEmail}')
BEGIN
    DECLARE @UserId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450))
    DECLARE @AdminRoleId NVARCHAR(450)
    
    SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin'
    
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount
    )
    VALUES (
        @UserId, 
        '{escapedEmail}', 
        UPPER('{escapedEmail}'), 
        '{escapedEmail}', 
        UPPER('{escapedEmail}'),
        1, 
        '{escapedHash}', 
        NEWID(), 
        NEWID(),
        0, 0, 1, 0
    )
    
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@UserId, @AdminRoleId)
    
    PRINT 'Admin user created'
END
ELSE
BEGIN
    PRINT 'User already exists'
END
";

Console.WriteLine("Executing SQL...");
var connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True";

using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();
    using (var command = new SqlCommand(sql, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
}

Console.WriteLine("? SQL executed successfully");
Console.WriteLine();
Console.WriteLine("Verifying...");

using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();
    using (var command = new SqlCommand($"SELECT Email, CASE WHEN PasswordHash IS NULL THEN 'NO' ELSE 'YES' END as HasPassword FROM AspNetUsers WHERE Email = '{escapedEmail}'", connection))
    {
        using (var reader = await command.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                Console.WriteLine($"? User found: {reader["Email"]}");
                Console.WriteLine($"  Has Password: {reader["HasPassword"]}");
            }
            else
            {
                Console.WriteLine("? User not found!");
            }
        }
    }
}
