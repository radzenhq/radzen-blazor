using System;

namespace Radzen.Documents.Layout;

internal struct LayoutProgressGuard(string work)
{
    private int position = -1;
    private int stalls;

    public void Reached(int position)
    {
        if (position != this.position)
        {
            this.position = position;
            stalls = 0;
            return;
        }

        if (++stalls <= 1)
        {
            return;
        }

        throw new InvalidOperationException(FormattableString.Invariant(
            $"{work} stopped making progress at {position}; every step must either place content or start a new page."));
    }
}
