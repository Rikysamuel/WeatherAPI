public class ZipCodeNotFoundException : NotFoundException
{
    public ZipCodeNotFoundException(string zipCode)
        : base($"Zip Code: '{zipCode}' was not found.")
    {
    }
}

public class LocationNotFoundException : NotFoundException
{
    public LocationNotFoundException(string cityName)
        : base($"City Name: '{cityName}' was not found.")
    {
    }
}

// Base exception
public abstract class NotFoundException : Exception
{
    protected NotFoundException(string message) : base(message)
    {
    }

    protected NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
