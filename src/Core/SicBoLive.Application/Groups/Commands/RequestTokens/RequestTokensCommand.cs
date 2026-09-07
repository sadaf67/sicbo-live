using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Application.Common.Models;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Groups.Commands.RequestTokens;

/// <summary>Player asks the table's dealer for a specific amount of tokens; the dealer approves
/// (which credits the wallet, same as IssueTokensCommand) or rejects via RespondTokenRequestCommand.</summary>
public record RequestTokensCommand(Guid GroupId, decimal Amount) : IRequest<Guid>;

public class RequestTokensCommandHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, IGameNotifier notifier, IUserDirectory userDirectory)
    : IRequestHandler<RequestTokensCommand, Guid>
{
    public async Task<Guid> Handle(RequestTokensCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Player)
            throw new UnauthorizedAccessException("فقط بازیکنان می‌توانند درخواست ژتون بدهند.");

        if (request.Amount <= 0)
            throw new ArgumentException("مقدار درخواستی باید مثبت باشد.");

        var group = await db.Groups.FindAsync([request.GroupId], cancellationToken)
            ?? throw new KeyNotFoundException("میز مورد نظر یافت نشد.");

        var wallet = await db.Wallets.FirstOrDefaultAsync(
            w => w.GroupId == request.GroupId && w.UserId == currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("قبل از درخواست ژتون باید به میز بپیوندید.");

        var hasPending = await db.TokenRequests.AnyAsync(
            r => r.GroupId == request.GroupId && r.PlayerId == currentUser.UserId && r.Status == TokenRequestStatus.Pending,
            cancellationToken);
        if (hasPending)
            throw new InvalidOperationException("شما یک درخواست ژتون در انتظار تأیید دارید.");

        var tokenRequest = new TokenRequest(request.GroupId, currentUser.UserId, request.Amount);
        db.TokenRequests.Add(tokenRequest);
        await db.SaveChangesAsync(cancellationToken);

        var names = await userDirectory.GetDisplayNamesAsync([currentUser.UserId], cancellationToken);
        var dto = new TokenRequestDto(
            tokenRequest.Id, tokenRequest.GroupId, tokenRequest.PlayerId,
            names.GetValueOrDefault(currentUser.UserId, "?"), tokenRequest.Amount,
            tokenRequest.Status.ToString(), tokenRequest.CreatedAt);
        await notifier.TokenRequested(dto, cancellationToken);

        return tokenRequest.Id;
    }
}
