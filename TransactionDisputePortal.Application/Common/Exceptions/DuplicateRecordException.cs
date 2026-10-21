namespace TransactionDisputePortal.Application.Common.Exceptions;

public class DuplicateRecordException : Exception
{
    public DuplicateRecordException(Exception innerException) : base("A record with the same unique values already exists.", innerException) { }
}
