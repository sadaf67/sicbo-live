using MediatR;
using Microsoft.EntityFrameworkCore;
using SicBoLive.Application.Common.Interfaces;
using SicBoLive.Domain.Enums;

namespace SicBoLive.Application.Admin.Commands.UpdateSubscriptionPlan;

public record UpdateSubscriptionPlanCommand(Guid PlanId, decimal EntryTokenAmount, int GameDurationMinutes, decimal DiscountPercent)
    : IRequest;

public class UpdateSubscriptionPlanCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateSubscriptionPlanCommand>
{
    public async Task Handle(UpdateSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("فقط ادمین می‌تواند طرح اشتراک را ویرایش کند.");

        var plan = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new KeyNotFoundException("طرح اشتراک مورد نظر یافت نشد.");

        plan.Update(request.EntryTokenAmount, request.GameDurationMinutes, request.DiscountPercent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
