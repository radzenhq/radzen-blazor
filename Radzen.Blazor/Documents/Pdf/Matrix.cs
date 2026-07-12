using System;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A 2D affine transform in the PDF matrix convention <c>[a b c d e f]</c>, mapping
/// a point <c>(x, y)</c> to <c>(a*x + c*y + e, b*x + d*y + f)</c>.
/// </summary>
public readonly struct Matrix : IEquatable<Matrix>
{
    private Matrix(double a, double b, double c, double d, double e, double f)
    {
        A = a;
        B = b;
        C = c;
        D = d;
        E = e;
        F = f;
    }

    /// <summary>Gets the <c>a</c> element (horizontal scaling).</summary>
    public double A { get; }

    /// <summary>Gets the <c>b</c> element (vertical shearing).</summary>
    public double B { get; }

    /// <summary>Gets the <c>c</c> element (horizontal shearing).</summary>
    public double C { get; }

    /// <summary>Gets the <c>d</c> element (vertical scaling).</summary>
    public double D { get; }

    /// <summary>Gets the <c>e</c> element (horizontal translation).</summary>
    public double E { get; }

    /// <summary>Gets the <c>f</c> element (vertical translation).</summary>
    public double F { get; }

    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Matrix Identity => new(1, 0, 0, 1, 0, 0);

    /// <summary>
    /// Creates a translation matrix.
    /// </summary>
    public static Matrix Translate(double tx, double ty) => new(1, 0, 0, 1, tx, ty);

    internal static Matrix FromComponents(double a, double b, double c, double d, double e, double f)
        => new(a, b, c, d, e, f);

    /// <summary>
    /// Creates a scaling matrix.
    /// </summary>
    public static Matrix Scale(double sx, double sy) => new(sx, 0, 0, sy, 0, 0);

    /// <summary>
    /// Creates a rotation matrix for the given angle in degrees (counterclockwise).
    /// </summary>
    public static Matrix Rotate(double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new(cos, sin, -sin, cos, 0, 0);
    }

    /// <summary>
    /// Applies this transform to the point <paramref name="x"/>, <paramref name="y"/>.
    /// </summary>
    public (double X, double Y) Transform(double x, double y)
        => (A * x + C * y + E, B * x + D * y + F);

    /// <summary>
    /// Composes two transforms. The <paramref name="left"/> transform is applied before the <paramref name="right"/> one.
    /// </summary>
    public static Matrix operator *(Matrix left, Matrix right) => new(
        left.A * right.A + left.B * right.C,
        left.A * right.B + left.B * right.D,
        left.C * right.A + left.D * right.C,
        left.C * right.B + left.D * right.D,
        left.E * right.A + left.F * right.C + right.E,
        left.E * right.B + left.F * right.D + right.F);

    /// <summary>
    /// Composes two transforms. The <paramref name="left"/> transform is applied before the <paramref name="right"/> one.
    /// </summary>
    public static Matrix Multiply(Matrix left, Matrix right) => left * right;

    /// <summary>
    /// Determines whether two matrices are equal.
    /// </summary>
    public static bool operator ==(Matrix left, Matrix right) => left.Equals(right);

    /// <summary>
    /// Determines whether two matrices are not equal.
    /// </summary>
    public static bool operator !=(Matrix left, Matrix right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(Matrix other)
        => A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C)
        && D.Equals(other.D) && E.Equals(other.E) && F.Equals(other.F);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Matrix other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(A, B, C, D, E, F);
}
