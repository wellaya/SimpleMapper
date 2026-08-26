namespace SimpleMapper;

public sealed class MappingException : Exception
{
    public MappingException(string message) : base(message) { }
    public MappingException(string message, Exception inner) : base(message, inner) { }
}