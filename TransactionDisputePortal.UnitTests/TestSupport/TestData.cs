using AutoMapper;
using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.UnitTests.TestSupport;

public static class TestData
{
    private static int sequence;

    public static IMapper CreateMapper()
    {
        return new MapperConfiguration(configuration => configuration.AddProfile<MappingProfile>()).CreateMapper();
    }

    public static Customer CreateCustomer(string fullName = "Test Customer")
    {
        return new Customer
        {
            ExternalUserId = Guid.NewGuid(),
            FullName = fullName,
            Email = $"{fullName.Replace(' ', '.').ToLowerInvariant()}@disputeportal.local"
        };
    }

    public static Account AddAccount(this Customer customer, AccountType type = AccountType.Cheque, string? accountNumber = null)
    {
        var account = new Account
        {
            Customer = customer,
            AccountNumber = accountNumber ?? $"62{Interlocked.Increment(ref sequence):D8}",
            Type = type
        };
        customer.Accounts.Add(account);

        return account;
    }

    public static Transaction AddTransaction(this Account account, DateTime transactionDate, string merchant = "Woolworths Food",
        TransactionStatus status = TransactionStatus.Posted, string? reference = null)
    {
        var transaction = new Transaction
        {
            Account = account,
            Reference = reference ?? $"TXN{transactionDate:yyyyMMdd}{Interlocked.Increment(ref sequence):D6}",
            Amount = 199.99m,
            Currency = "ZAR",
            Merchant = merchant,
            Category = TransactionCategory.Groceries,
            TransactionDate = transactionDate,
            Status = status
        };
        account.Transactions.Add(transaction);

        return transaction;
    }
}
