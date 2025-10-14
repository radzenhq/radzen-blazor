#nullable enable

using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

static class AggregationMethods
{
    public static CellData Sum(List<double> items)
    {
        double sum = 0;
        foreach (var d in items)
        {
            sum += d;
        }
        return CellData.FromNumber(sum);
    }

    public static CellData Average(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromError(CellError.Div0);
        }

        double sum = 0;
        foreach (var d in items)
        {
            sum += d;
        }
        return CellData.FromNumber(sum / items.Count);
    }

    public static CellData Max(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var max = items[0];
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i] > max)
            {
                max = items[i];
            }
        }
        return CellData.FromNumber(max);
    }

    public static CellData Min(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromNumber(0);
        }

        var min = items[0];
        for (int i = 1; i < items.Count; i++)
        {
            if (items[i] < min)
            {
                min = items[i];
            }
        }
        return CellData.FromNumber(min);
    }

    public static CellData Median(List<double> items)
    {
        if (items.Count == 0)
        {
            return CellData.FromNumber(0);
        }
        items.Sort();
        var n = items.Count;
        if (n % 2 == 1)
        {
            return CellData.FromNumber(items[n / 2]);
        }
        return CellData.FromNumber((items[n / 2 - 1] + items[n / 2]) / 2.0);
    }

    public static CellData Large(List<double> items, int k)
    {
        if (items.Count == 0 || k <= 0 || k > items.Count)
        {
            return CellData.FromError(CellError.Num);
        }
        items.Sort((a, b) => b.CompareTo(a));
        return CellData.FromNumber(items[k - 1]);
    }

    public static CellData Small(List<double> items, int k)
    {
        if (items.Count == 0 || k <= 0 || k > items.Count)
        {
            return CellData.FromError(CellError.Num);
        }
        items.Sort();
        return CellData.FromNumber(items[k - 1]);
    }
}


