using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Commands.DeactivateSubscriptionPlan;

public record DeactivateSubscriptionPlanCommand(Guid PlanId) : IRequest;

public class DeactivateSubscriptionPlanCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<DeactivateSubscriptionPlanCommand>
{
    public async Task Handle(DeactivateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند طرح اشتراک را غیرفعال کند.");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException("طرح اشتراک مورد نظر یافت نشد.");

        plan.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
    }
}
