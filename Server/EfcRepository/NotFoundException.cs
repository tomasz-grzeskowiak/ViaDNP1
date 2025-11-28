namespace EfcRepository;

public class NotFoundException : Exception
{
    public override string Message { get; } = "";

    public NotFoundException(string message)
    {
        this.Message = message;
    }

    public NotFoundException()
    {
    }
}