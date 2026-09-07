namespace SicBoLive.Application.Common.Models;

public record TokenRequestDto(
    Guid RequestId,
    Guid GroupId,
    Guid PlayerId,
    string PlayerDisplayName,
    decimal Amount,
    string Status,
    DateTimeOffset RequestedAt);
