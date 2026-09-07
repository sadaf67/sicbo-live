using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;
using SicBoLive.Infrastructure.Persistence;

namespace SicBoLive.Infrastructure.Services;

public class UserDirectory(ApplicationDbContext db) : IUserDirectory
{
    public async Task<Dictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        return await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerDirectoryEntry>> GetPlayersAsync(CancellationToken cancellationToken = default)
    {
        return await db.Users
            .Where(u => u.Role == UserRole.Player)
            .OrderBy(u => u.DisplayName)
            .Select(u => new PlayerDirectoryEntry(u.Id, u.DisplayName, u.Email ?? string.Empty, u.VerificationPhoto != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]?> GetVerificationPhotoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.VerificationPhoto)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
