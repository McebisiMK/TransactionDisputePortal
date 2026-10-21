using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

internal static class CustomerSeedData
{
    private const string Currency = "ZAR";
    private const int HistoryDays = 150;
    private const int PendingDays = 3;

    private sealed record SeedCustomer(int Number, Guid ExternalUserId, string Username, string FullName, string Email, SeedAccount[] Accounts);

    private sealed record SeedAccount(string AccountNumber, AccountType Type, int TransactionCount);

    private sealed record SeedMerchant(string Name, TransactionCategory Category, decimal MinAmount, decimal MaxAmount);

    private static readonly SeedCustomer[] Customers =
    [
        new(1, Guid.Parse("11111111-1111-1111-1111-111111111111"), "test.customer", "Test Customer", "test.customer@disputeportal.local",
        [
            new("******4521", AccountType.Cheque, 30),
            new("******8834", AccountType.CreditCard, 20)
        ]),
        new(2, Guid.Parse("33333333-3333-3333-3333-333333333333"), "test.customer2", "Second Customer", "test.customer2@disputeportal.local",
        [
            new("******7310", AccountType.Cheque, 25),
            new("******6092", AccountType.CreditCard, 25)
        ])
    ];

    private static readonly SeedMerchant[] Merchants =
    [
        new("Woolworths Food", TransactionCategory.Groceries, 150m, 1800m),
        new("Checkers Hyper", TransactionCategory.Groceries, 200m, 2200m),
        new("Pick n Pay", TransactionCategory.Groceries, 80m, 1500m),
        new("Engen Garage", TransactionCategory.Fuel, 400m, 1200m),
        new("Shell Ultra City", TransactionCategory.Fuel, 450m, 1300m),
        new("Nando's", TransactionCategory.Dining, 90m, 450m),
        new("Ocean Basket", TransactionCategory.Dining, 250m, 900m),
        new("Vida e Caffe", TransactionCategory.Dining, 40m, 160m),
        new("Uber Trip", TransactionCategory.Transport, 45m, 350m),
        new("Gautrain", TransactionCategory.Transport, 60m, 180m),
        new("Takealot.com", TransactionCategory.Shopping, 150m, 4500m),
        new("Mr Price", TransactionCategory.Shopping, 120m, 1200m),
        new("Ster-Kinekor", TransactionCategory.Entertainment, 90m, 400m),
        new("Netflix", TransactionCategory.Subscriptions, 199m, 199m),
        new("Spotify", TransactionCategory.Subscriptions, 69.99m, 69.99m),
        new("Vodacom Airtime", TransactionCategory.Utilities, 50m, 500m),
        new("City Power Prepaid", TransactionCategory.Utilities, 300m, 1500m),
        new("Dis-Chem Pharmacy", TransactionCategory.Health, 80m, 950m),
        new("FlySafair", TransactionCategory.Travel, 1200m, 4800m),
        new("ATM Withdrawal", TransactionCategory.CashWithdrawal, 200m, 2000m)
    ];

    public static IEnumerable<Guid> ExternalUserIds => Customers.Select(customer => customer.ExternalUserId);

    public static string GetUsername(Guid externalUserId) => Customers.Single(customer => customer.ExternalUserId == externalUserId).Username;

    public static Customer CreateCustomer(Guid externalUserId, DateTime utcNow)
    {
        var seed = Customers.Single(customer => customer.ExternalUserId == externalUserId);
        var random = new Random(seed.Number);

        return new Customer
        {
            ExternalUserId = seed.ExternalUserId,
            FullName = seed.FullName,
            Email = seed.Email,
            Accounts = seed.Accounts
                .Select((account, accountIndex) => new Account
                {
                    AccountNumber = account.AccountNumber,
                    Type = account.Type,
                    Transactions = Enumerable.Range(0, account.TransactionCount)
                        .Select(index => CreateTransaction(random, utcNow, $"{seed.Number}{accountIndex}{index:D3}", index))
                        .ToList()
                })
                .ToList()
        };
    }

    private static Transaction CreateTransaction(Random random, DateTime utcNow, string sequence, int index)
    {
        var merchant = Merchants[random.Next(Merchants.Length)];

        var daysAgo = index < PendingDays ? index : random.Next(PendingDays, HistoryDays);
        var transactionDate = utcNow.Date.AddDays(-daysAgo).AddHours(random.Next(6, 22)).AddMinutes(random.Next(60));
        if (transactionDate > utcNow) transactionDate = utcNow.AddMinutes(-random.Next(1, 60));

        var amount = merchant.MinAmount == merchant.MaxAmount
            ? merchant.MinAmount
            : Math.Round(merchant.MinAmount + (decimal)random.NextDouble() * (merchant.MaxAmount - merchant.MinAmount), 2);

        return new Transaction
        {
            Reference = $"TXN{transactionDate:yyyyMMdd}{sequence}",
            Amount = amount,
            Currency = Currency,
            Merchant = merchant.Name,
            Category = merchant.Category,
            TransactionDate = transactionDate,
            Status = daysAgo < PendingDays ? TransactionStatus.Pending : TransactionStatus.Posted
        };
    }
}
