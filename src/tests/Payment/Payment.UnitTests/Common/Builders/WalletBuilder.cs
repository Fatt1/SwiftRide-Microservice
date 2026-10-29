using Payment.Domain.Entities;

namespace Payment.UnitTests.Common.Builders;

public class WalletBuilder
{
    private Guid _userId = Guid.NewGuid();
    private decimal _balance = 500_000m;
    private string _currency = "VND";

    public WalletBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public WalletBuilder WithBalance(decimal balance)
    {
        _balance = balance;
        return this;
    }

    public WalletBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public Wallet Build()
    {
        return Wallet.Create(_userId, _balance, _currency);
    }
}
