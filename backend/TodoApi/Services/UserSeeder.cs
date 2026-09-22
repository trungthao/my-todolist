using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services;

public static class UserSeeder
{
    public static async Task SeedDefaultUserAsync(AppDbContext db, IConfiguration config)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var username = config["Auth:DefaultUsername"] ?? "admin";
        var password = config["Auth:DefaultPassword"] ?? "changeme123";

        db.Users.Add(new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        });

        await db.SaveChangesAsync();
    }
}
