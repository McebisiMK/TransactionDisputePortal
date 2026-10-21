namespace TransactionDisputePortal.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    private const string DefaultMessage = "The record was modified by another request. Please reload and try again.";

    public ConcurrencyException() : base(DefaultMessage) { }

    public ConcurrencyException(Exception innerException) : base(DefaultMessage, innerException) { }
}
