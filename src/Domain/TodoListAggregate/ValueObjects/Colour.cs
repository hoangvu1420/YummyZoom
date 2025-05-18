namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public class Color : ValueObject
{
    public string Code { get; private set; }

    private Color(string code)
    {
        Code = code;
    }

    public static Color Create(string code)
    {
        return string.IsNullOrWhiteSpace(code) ? new Color("#000000") : new Color(code);
    }

    public static Color White => new("#FFFFFF");

    public static Color Red => new("#FF5733");

    public static Color Orange => new("#FFC300");

    public static Color Yellow => new("#FFFF66");

    public static Color Green => new("#CCFF99");

    public static Color Blue => new("#6666FF");

    public static Color Purple => new("#9966CC");

    public static Color Grey => new("#999999");

    public static implicit operator string(Color colour)
    {
        return colour.ToString();
    }

    public static explicit operator Color(string code)
    {
        return Create(code);
    }

    public override string ToString()
    {
        return Code;
    }

    protected static IEnumerable<Color> SupportedColours
    {
        get
        {
            yield return White;
            yield return Red;
            yield return Orange;
            yield return Yellow;
            yield return Green;
            yield return Blue;
            yield return Purple;
            yield return Grey;
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
    }

#pragma warning disable CS8618
    private Color()
    {
    }
#pragma warning restore CS8618
}
