namespace TransactionDisputePortal.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException() : base() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

    public NotFoundException(string name, object key) : base(CreateMessage(name, key)) { }

    public static string CreateMessage(string name, object key) => $"Entity \"{name}\" ({key}) was not found.";
}
