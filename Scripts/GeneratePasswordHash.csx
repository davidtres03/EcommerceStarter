using Microsoft.AspNetCore.Identity;

var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, "Test@123");
Console.WriteLine($"Password Hash: {hash}");
Console.WriteLine($"Hash Length: {hash.Length}");
