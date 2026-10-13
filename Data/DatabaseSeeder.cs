
using MongoDB.Driver;
using ProjectFlow.Api.Models;

namespace ProjectFlow.Api.Data; 

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        MongoDbContext db)
    {
        Console.WriteLine("DATABASE SEEDER");

        
        // ADMIN
        
        var adminExists = await db.Users
            .Find(x => x.Email == "admin@projectflow.com")
            .AnyAsync();

        if (!adminExists)
        {
            var admin = new User
            {
                Name = "ProjectFlow Admin",
                Email = "admin@projectflow.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    "Admin@12345"
                ),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.InsertOneAsync(admin);

            Console.WriteLine(
                "Admin user created: admin@projectflow.com"
            );
        }
        else
        {
            Console.WriteLine(
                "Admin user already exists."
            );
        }


        
        // MANAGER
        
        var managerExists = await db.Users
            .Find(x => x.Email == "manager@projectflow.com")
            .AnyAsync();

        if (!managerExists)
        {
            var manager = new User
            {
                Name = "ProjectFlow Manager",
                Email = "manager@projectflow.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    "Manager@12345"
                ),
                Role = "Manager",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.InsertOneAsync(manager);

            Console.WriteLine(
                "Manager user created: manager@projectflow.com"
            );
        }
        else
        {
            Console.WriteLine(
                "Manager user already exists."
            );
        }


        
        // DEVELOPER
        
        var employeeExists = await db.Users
            .Find(x => x.Email == "employee@projectflow.com")
            .AnyAsync();

        if (!employeeExists)
        {
            var employee = new User
            {
                Name = "ProjectFlow employee",
                Email = "employee@projectflow.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    "Employee@12345"
                ),
                Role = "Employee",
                CreatedAt = DateTime.UtcNow
            };

            await db.Users.InsertOneAsync(employee);

            Console.WriteLine(
                "Employee user created: employee@projectflow.com"
            );
        }
        else
        {
            Console.WriteLine(
                "Employee user already exists."
            );
        }


        
        Console.WriteLine("Database seeding completed.");
        
    }
}
