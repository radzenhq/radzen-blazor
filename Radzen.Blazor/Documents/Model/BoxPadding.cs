namespace Radzen.Documents;

internal readonly record struct BoxPadding(double Left, double Right, double Top, double Bottom)
{
    public double Horizontal => Left + Right;

    public double Vertical => Top + Bottom;
}
