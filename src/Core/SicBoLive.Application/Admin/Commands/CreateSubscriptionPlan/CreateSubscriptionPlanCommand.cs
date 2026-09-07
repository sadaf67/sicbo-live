using MediatR;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Entities;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Commands.CreateSubscriptionPlan;

public record CreateSubscriptionPlanCommand(string Name, decimal EntryTokenAmount, int GameDurationMinutes, decimal DiscountPercent)
    : IRequest<Guid>;

public class CreateSubscriptionPlanCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<CreateSubscriptionPlanCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند طرح اشتراک ایجاد کند.");

        var plan = new SubscriptionPlan(request.Name, request.EntryTokenAmount, request.GameDurationMinutes, request.DiscountPercent);
        db.SubscriptionPlans.Add(plan);

        await db.SaveChangesAsync(cancellationToken);
        return plan.Id;
    }
}
