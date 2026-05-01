namespace WeatherApi.Core.Exceptions;

public class ZipCodeNotFoundException(string zipCode) : NotFoundException($"Zip Code: '{zipCode}' was not found.")
{ }

public class LocationNotFoundException(string cityName) : NotFoundException($"City Name: '{cityName}' was not found.")
{ }

// Base exception
public abstract class NotFoundException(string message) : Exception(message)
{ }