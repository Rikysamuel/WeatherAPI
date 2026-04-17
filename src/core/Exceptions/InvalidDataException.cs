public class InvalidDataException : InvalidOperationException
{
    public InvalidDataException(string message)
        : base(message)
    {
    }
}