using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FilteringTests
{
    private readonly Sheet sheet = new(10, 10);

    [Fact]
    public void Should_FilterEqualsCriterion()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        sheet.Filter(
            RangeRef.Parse("A1:A3"),
            new EqualsCriterion { ColumnIndex = 0, Value = "A" }
        );

        Assert.False(sheet.Rows.IsHidden(0));
        Assert.True(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_ShowRowsAfterApplyingDifferentCriterion()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        sheet.Filter(
            RangeRef.Parse("A1:A3"),
            new EqualsCriterion { ColumnIndex = 0, Value = "A" }
        );

        Assert.False(sheet.Rows.IsHidden(0));
        Assert.True(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));

        // Apply a different criterion
        sheet.Filter(
            RangeRef.Parse("A1:A3"),
            new EqualsCriterion { ColumnIndex = 0, Value = "B" }
        );

        Assert.True(sheet.Rows.IsHidden(0));
        Assert.False(sheet.Rows.IsHidden(1));
        Assert.True(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_TreatNumericStringsEqualToNumbers()
    {
        var sheet = new Sheet(4, 1);
        sheet.Cells[0, 0].Value = "10";    // row 0: string
        sheet.Cells[1, 0].Value = 10;      // row 1: number
        sheet.Cells[2, 0].Value = "10.0";  // row 2: string
        sheet.Cells[3, 0].Value = "abc";   // row 3: non-numeric string

        sheet.Filter(
            RangeRef.Parse("A1:A4"), // includes rows 0–3
            new EqualsCriterion { ColumnIndex = 0, Value = 10 }
        );

        Assert.False(sheet.Rows.IsHidden(0)); // "10"
        Assert.False(sheet.Rows.IsHidden(1)); // 10
        Assert.False(sheet.Rows.IsHidden(2)); // "10.0"
        Assert.True(sheet.Rows.IsHidden(3));  // "abc" → not a number
    }

    [Fact]
    public void Should_FilterWithOrCriterion()
    {
        // Setup:
        // Row 0: ignored (header)
        // Row 1: A=Active, B=20
        // Row 2: A=Pending, B=70
        // Row 3: A=Inactive, B=90

        sheet.Cells[1, 0].Value = "Active";
        sheet.Cells[1, 1].Value = 20;
        sheet.Cells[2, 0].Value = "Pending";
        sheet.Cells[2, 1].Value = 70;
        sheet.Cells[3, 0].Value = "Inactive";
        sheet.Cells[3, 1].Value = 90;

        sheet.Filter(RangeRef.Parse("A2:B4"), new OrCriterion
        {
            Criteria = [
                new EqualsCriterion { ColumnIndex = 0, Value = "Pending" },
            new EqualsCriterion { ColumnIndex = 1, Value = 90 }
            ]
        });

        Assert.True(sheet.Rows.IsHidden(1));  // A=Active, B=20 — doesn't match either
        Assert.False(sheet.Rows.IsHidden(2)); // A=Pending — matches
        Assert.False(sheet.Rows.IsHidden(3)); // B=90 — matches
    }

    [Fact]
    public void Should_FilterWithAndCriterion()
    {
        var sheet = new Sheet(4, 2);

        sheet.Cells[1, 0].Value = "Active"; sheet.Cells[1, 1].Value = 85;
        sheet.Cells[2, 0].Value = "Active"; sheet.Cells[2, 1].Value = 60;
        sheet.Cells[3, 0].Value = "Inactive"; sheet.Cells[3, 1].Value = 90;

        sheet.Filter(RangeRef.Parse("A2:B4"), new AndCriterion
        {
            Criteria = [
                new EqualsCriterion { ColumnIndex = 0, Value = "Active" },
                new EqualsCriterion { ColumnIndex = 1, Value = 85 }
            ]
        });

        Assert.False(sheet.Rows.IsHidden(1)); // Matches both
        Assert.True(sheet.Rows.IsHidden(2));  // B too low
        Assert.True(sheet.Rows.IsHidden(3));  // Not Active
    }


    [Fact]
    public void Should_FilterWithNestedOrInAndCriterion()
    {
        var sheet = new Sheet(5, 2);

        sheet.Cells[1, 0].Value = "Active"; sheet.Cells[1, 1].Value = 90;
        sheet.Cells[2, 0].Value = "Pending"; sheet.Cells[2, 1].Value = 45;
        sheet.Cells[3, 0].Value = "Pending"; sheet.Cells[3, 1].Value = 95;
        sheet.Cells[4, 0].Value = "Inactive"; sheet.Cells[4, 1].Value = 100;

        sheet.Filter(RangeRef.Parse("A2:B5"), new AndCriterion
        {
            Criteria = [
                new OrCriterion {
                Criteria = [
                    new EqualsCriterion { ColumnIndex = 0, Value = "Active" },
                    new EqualsCriterion { ColumnIndex = 0, Value = "Pending" }
                ]
            },
            new GreaterThanCriterion {
                ColumnIndex = 1, Value = 80
            }
            ]
        });

        Assert.False(sheet.Rows.IsHidden(1)); // Active + 90
        Assert.True(sheet.Rows.IsHidden(2));  // Pending + 45 (too low)
        Assert.False(sheet.Rows.IsHidden(3)); // Pending + 95
        Assert.True(sheet.Rows.IsHidden(4));  // Inactive
    }
}