using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SerialDateTests
{
    [Fact]
    public void Jan1_1900_ShouldBeSerial1()
    {
        Assert.Equal(1d, new DateTime(1900, 1, 1).ToNumber());
    }

    [Fact]
    public void Jan2_1900_ShouldBeSerial2()
    {
        Assert.Equal(2d, new DateTime(1900, 1, 2).ToNumber());
    }

    [Fact]
    public void Feb28_1900_ShouldBeSerial59()
    {
        Assert.Equal(59d, new DateTime(1900, 2, 28).ToNumber());
    }

    [Fact]
    public void Mar1_1900_ShouldBeSerial61()
    {
        // Serial 60 is the phantom Feb 29, 1900 (Lotus 1-2-3 bug)
        Assert.Equal(61d, new DateTime(1900, 3, 1).ToNumber());
    }

    [Fact]
    public void Jan1_2024_ShouldBeSerial45292()
    {
        Assert.Equal(45292d, new DateTime(2024, 1, 1).ToNumber());
    }

    [Fact]
    public void Serial1_ShouldBeJan1_1900()
    {
        Assert.Equal(new DateTime(1900, 1, 1), 1d.ToDate());
    }

    [Fact]
    public void Serial61_ShouldBeMar1_1900()
    {
        Assert.Equal(new DateTime(1900, 3, 1), 61d.ToDate());
    }

    [Fact]
    public void Serial45292_ShouldBeJan1_2024()
    {
        Assert.Equal(new DateTime(2024, 1, 1), 45292d.ToDate());
    }

    [Fact]
    public void RoundTripShouldBeConsistent()
    {
        var date = new DateTime(2024, 6, 15, 12, 30, 0);
        var serial = date.ToNumber();
        var roundTripped = serial.ToDate();

        Assert.Equal(date.Date, roundTripped.Date);
    }
}
