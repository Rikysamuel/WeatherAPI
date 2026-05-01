namespace WeatherApi.Core.Exceptions;

public class InvalidDataException(string message) : InvalidOperationException(message)
{ }
