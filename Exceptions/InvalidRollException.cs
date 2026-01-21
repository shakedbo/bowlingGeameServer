namespace BowlingGame.API.Exceptions;

public class InvalidRollException : Exception
{
    public int Pins { get; }

    public InvalidRollException(string message) : base(message)
    {
    }

    public InvalidRollException(int pins, string reason)
        : base($"Invalid roll of {pins} pins: {reason}")
    {
        Pins = pins;
    }
}
