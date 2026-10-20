namespace TransactionDisputePortal.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException() : base("The record was modified by another request. Please reload and try again.") { }
}
