namespace SicBoLive.Application.Common.Interfaces;

public record PlayerDirectoryEntry(Guid UserId, string DisplayName, string Email, bool HasVerificationPhoto);

public interface IUserDirectory
{
    Task<Dictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>All users with the Player role, for the admin "users" panel.</summary>
    Task<IReadOnlyList<PlayerDirectoryEntry>> GetPlayersAsync(CancellationToken cancellationToken = default);

    /// <summary>Raw JPEG bytes of the mandatory registration-time selfie, or null if the account
    /// predates the identity-verification feature. Admin-only use (see GetPlayerVerificationPhotoQuery).</summary>
    Task<byte[]?> GetVerificationPhotoAsync(Guid userId, CancellationToken cancellationToken = default);
}
