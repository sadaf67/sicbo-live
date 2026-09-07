using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;
using SicBoLive.Domain.Seed;
using SicBoLive.Infrastructure.Identity;

namespace SicBoLive.Infrastructure.Persistence;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        await db.Database.MigrateAsync();

        var existingCodes = await db.BetTypeConfigs.Select(b => b.Code).ToListAsync();
        var missingBetTypes = StandardSicBoBoard.CreateDefaultBetTypes()
            .Where(b => !existingCodes.Contains(b.Code))
            .ToList();
        if (missingBetTypes.Count > 0)
        {
            db.BetTypeConfigs.AddRange(missingBetTypes);
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var admin = new ApplicationUser
            {
                UserName = "admin@sicbolive.local",
                Email = "admin@sicbolive.local",
                DisplayName = "مدیر سیستم",
                Role = UserRole.Admin,
            };
            await userManager.CreateAsync(admin, "Admin@12345");
        }

        // Standard 1/2/3-month subscription plans, priced to match the amounts quoted to players in
        // the Telegram bot's /start message (400/700/900 ژتون) - keeps the admin panel's plan picker
        // and the bot's advertised prices in sync. Matched by Name so re-running the seeder on an
        // existing database won't duplicate a plan the admin already has (e.g. after an upgrade); if
        // an admin deliberately deactivates/renames one of these, the seeder leaves it alone and won't
        // recreate it since the exact name won't match anymore.
        var existingPlanNames = await db.SubscriptionPlans.Select(p => p.Name).ToListAsync();
        var standardPlans = new[]
        {
            new SubscriptionPlan("اشتراک یک ماهه", 400, 30 * 24 * 60),
            new SubscriptionPlan("اشتراک دو ماهه", 700, 60 * 24 * 60),
            new SubscriptionPlan("اشتراک سه ماهه", 900, 90 * 24 * 60),
        };
        var missingPlans = standardPlans.Where(p => !existingPlanNames.Contains(p.Name)).ToList();
        if (missingPlans.Count > 0)
        {
            db.SubscriptionPlans.AddRange(missingPlans);
            await db.SaveChangesAsync();
        }
    }
}
