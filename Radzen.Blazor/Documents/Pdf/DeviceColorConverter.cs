using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal static class DeviceColorConverter
{
    public static Color FromComponents(IReadOnlyList<double> values) => values.Count switch
    {
        1 => Color.FromRgb(Channel(values[0]), Channel(values[0]), Channel(values[0])),
        3 => Color.FromRgb(Channel(values[0]), Channel(values[1]), Channel(values[2])),
        4 => Color.FromRgb(
            Channel((1 - values[0]) * (1 - values[3])),
            Channel((1 - values[1]) * (1 - values[3])),
            Channel((1 - values[2]) * (1 - values[3]))),
        _ => throw new ArgumentException("A device colour requires one, three or four components.", nameof(values)),
    };

    private static byte Channel(double value) => ColorComponent.ToChannel(value);
}
