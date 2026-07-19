#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class MatrixTests
{
    [Fact]
    public void Identity_Elements()
    {
        var m = Matrix.Identity;
        Assert.Equal(1, m.A, 12);
        Assert.Equal(0, m.B, 12);
        Assert.Equal(0, m.C, 12);
        Assert.Equal(1, m.D, 12);
        Assert.Equal(0, m.E, 12);
        Assert.Equal(0, m.F, 12);
    }

    [Fact]
    public void Identity_TransformIsNoOp()
    {
        var p = Matrix.Identity.Transform(3, 4);
        Assert.Equal(3, p.X, 12);
        Assert.Equal(4, p.Y, 12);
    }

    [Fact]
    public void Translate_Elements()
    {
        var m = Matrix.Translate(10, 20);
        Assert.Equal(1, m.A, 12);
        Assert.Equal(1, m.D, 12);
        Assert.Equal(10, m.E, 12);
        Assert.Equal(20, m.F, 12);
    }

    [Fact]
    public void Translate_Transform()
    {
        var p = Matrix.Translate(10, 20).Transform(3, 4);
        Assert.Equal(13, p.X, 12);
        Assert.Equal(24, p.Y, 12);
    }

    [Fact]
    public void Scale_Elements()
    {
        var m = Matrix.Scale(2, 3);
        Assert.Equal(2, m.A, 12);
        Assert.Equal(3, m.D, 12);
        Assert.Equal(0, m.E, 12);
        Assert.Equal(0, m.F, 12);
    }

    [Fact]
    public void Scale_Transform()
    {
        var p = Matrix.Scale(2, 3).Transform(3, 4);
        Assert.Equal(6, p.X, 12);
        Assert.Equal(12, p.Y, 12);
    }

    [Fact]
    public void Rotate90_Elements()
    {
        var m = Matrix.Rotate(90);
        Assert.Equal(0, m.A, 12);
        Assert.Equal(1, m.B, 12);
        Assert.Equal(-1, m.C, 12);
        Assert.Equal(0, m.D, 12);
    }

    [Fact]
    public void Rotate90_TransformMapsXToY()
    {
        var p = Matrix.Rotate(90).Transform(10, 0);
        Assert.Equal(0, p.X, 9);
        Assert.Equal(10, p.Y, 9);
    }

    [Fact]
    public void Multiply_AppliesLeftThenRight()
    {
        var m = Matrix.Translate(10, 0) * Matrix.Rotate(90);
        var p = m.Transform(0, 0);
        Assert.Equal(0, p.X, 9);
        Assert.Equal(10, p.Y, 9);
    }

    [Fact]
    public void Multiply_Translations_Add()
    {
        Assert.Equal(Matrix.Translate(4, 6), Matrix.Translate(1, 2) * Matrix.Translate(3, 4));
    }

}
