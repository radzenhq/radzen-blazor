namespace Radzen.Documents.Layout;

internal readonly struct GradientReference
{
    private readonly double width;
    private readonly double height;
    private readonly bool sized;

    private GradientReference(double width, double height)
    {
        this.width = width;
        this.height = height;
        sized = true;
    }

    public static GradientReference Box(double width, double height) => new(width, height);

    public static GradientReference None => default;

    public double X(Unit value) => Resolve(value, width);

    public double Y(Unit value) => Resolve(value, height);

    public double Radius(Unit value) => Resolve(value, width);

    private double Resolve(Unit value, double extent) => sized ? value.Resolve(extent) : value.Point;
}
