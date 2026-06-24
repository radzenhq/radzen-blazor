namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Generates SVG path data for chart marker shapes. Shared by the series markers and the
    /// active point highlight so both always render the same geometry.
    /// </summary>
    public static class MarkerPath
    {
        /// <summary>
        /// Returns the SVG path data for the specified marker shape centered at (<paramref name="x" />, <paramref name="y" />).
        /// Returns an empty string for shapes which render as a primitive instead of a path (e.g. <see cref="MarkerType.Circle" />).
        /// </summary>
        /// <param name="type">The marker shape.</param>
        /// <param name="x">The center X coordinate.</param>
        /// <param name="y">The center Y coordinate.</param>
        /// <param name="size">Half the width and height of the shape.</param>
        public static string For(MarkerType type, double x, double y, double size)
        {
            return type switch
            {
                MarkerType.Square => $"M {(x - size).ToInvariantString()} {(y - size).ToInvariantString()} L {(x + size).ToInvariantString()} {(y - size).ToInvariantString()} L {(x + size).ToInvariantString()} {(y + size).ToInvariantString()} L {(x - size).ToInvariantString()} {(y + size).ToInvariantString()} Z",
                MarkerType.Triangle => $"M {(x - size).ToInvariantString()} {(y + size).ToInvariantString()} L {x.ToInvariantString()} {(y - size).ToInvariantString()} L {(x + size).ToInvariantString()} {(y + size).ToInvariantString()} Z",
                MarkerType.Diamond => $"M {(x - size).ToInvariantString()} {y.ToInvariantString()} L {x.ToInvariantString()} {(y - size).ToInvariantString()} L {(x + size).ToInvariantString()} {y.ToInvariantString()} L {x.ToInvariantString()} {(y + size).ToInvariantString()} Z",
                _ => string.Empty,
            };
        }
    }
}
