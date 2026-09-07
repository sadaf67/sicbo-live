using SicBoLive.Domain.Entities;
using Xunit;

namespace SicBoLive.Domain.Tests;

public class WalletTests
{
    [Fact]
    public void Debit_ThrowsWhenAmountExceedsBalance()
    {
        var wallet = new Wallet(Guid.NewGuid(), Guid.NewGuid());
        wallet.Credit(50);

        Assert.Throws<InvalidOperationException>(() => wallet.Debit(51));
        Assert.Equal(50, wallet.Balance); // unchanged - a player's balance must never go negative
    }

    [Fact]
    public void Debit_AllowsExactBalance()
    {
        var wallet = new Wallet(Guid.NewGuid(), Guid.NewGuid());
        wallet.Credit(50);

        wallet.Debit(50);
        Assert.Equal(0, wallet.Balance);
    }

    // DebitAllowingNegative is used only for the dealer's "house" wallet paying out a player's
    // net winnings (see RollDiceCommandHandler) - unlike a player wallet, the dealer's virtual
    // balance is allowed to go negative since it's never real currency and never cashed out.
    [Fact]
    public void DebitAllowingNegative_AllowsBalanceToGoNegative()
    {
        var dealerWallet = new Wallet(Guid.NewGuid(), Guid.NewGuid());
        dealerWallet.Credit(10);

        dealerWallet.DebitAllowingNegative(150);

        Assert.Equal(-140, dealerWallet.Balance);
    }

    [Fact]
    public void DebitAllowingNegative_ThrowsOnNegativeAmount()
    {
        var wallet = new Wallet(Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => wallet.DebitAllowingNegative(-1));
    }
}
