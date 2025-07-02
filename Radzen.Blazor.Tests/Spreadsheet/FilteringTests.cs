using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FilteringTests
{
    private readonly Sheet sheet = new(10, 10);

    [Fact]
    public void Should_FilterEqualToCriterion()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

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

        var filterA = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filterA);

        Assert.True(sheet.Rows.IsHidden(1)); // row 1 hidden by filterA

        // Remove previous filter and apply new one
        sheet.RemoveFilter(filterA);
        var filterB = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "B" },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filterB);

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

        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = 10 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

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

        var filter = new SheetFilter(
            new OrCriterion
            {
                Criteria = [
                    new EqualToCriterion { Column = 0, Value = "Pending" },
                    new EqualToCriterion { Column = 1, Value = 90 }
                ]
            },
            RangeRef.Parse("A2:B4")
        );
        sheet.AddFilter(filter);

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

        var filter = new SheetFilter(
            new AndCriterion
            {
                Criteria = [
                    new EqualToCriterion { Column = 0, Value = "Active" },
                    new EqualToCriterion { Column = 1, Value = 85 }
                ]
            },
            RangeRef.Parse("A2:B4")
        );
        sheet.AddFilter(filter);

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

        var filter = new SheetFilter(
            new AndCriterion
            {
                Criteria = [
                    new OrCriterion {
                        Criteria = [
                            new EqualToCriterion { Column = 0, Value = "Active" },
                            new EqualToCriterion { Column = 0, Value = "Pending" }
                        ]
                    },
                    new GreaterThanCriterion {
                        Column = 1, Value = 80
                    }
                ]
            },
            RangeRef.Parse("A2:B5")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(1)); // Active + 90
        Assert.True(sheet.Rows.IsHidden(2));  // Pending + 45 (too low)
        Assert.False(sheet.Rows.IsHidden(3)); // Pending + 95
        Assert.True(sheet.Rows.IsHidden(4));  // Inactive
    }

    [Fact]
    public void Should_FilterWithInListCriterion()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = "Cherry";
        sheet.Cells[3, 0].Value = "Date";

        var filter = new SheetFilter(
            new InListCriterion { Column = 0, Values = ["Apple", "Cherry"] },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // Apple - matches
        Assert.True(sheet.Rows.IsHidden(1));  // Banana - doesn't match
        Assert.False(sheet.Rows.IsHidden(2)); // Cherry - matches
        Assert.True(sheet.Rows.IsHidden(3));  // Date - doesn't match
    }

    [Fact]
    public void Should_FilterWithInListCriterionForNumbers()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;
        sheet.Cells[2, 0].Value = 30;
        sheet.Cells[3, 0].Value = 40;

        var filter = new SheetFilter(
            new InListCriterion { Column = 0, Values = [10, 30, 50] },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // 10 - matches
        Assert.True(sheet.Rows.IsHidden(1));  // 20 - doesn't match
        Assert.False(sheet.Rows.IsHidden(2)); // 30 - matches
        Assert.True(sheet.Rows.IsHidden(3));  // 40 - doesn't match
    }

    [Fact]
    public void Should_FilterWithInListCriterionForMixedTypes()
    {
        sheet.Cells[0, 0].Value = "10";    // string
        sheet.Cells[1, 0].Value = 20;      // number
        sheet.Cells[2, 0].Value = "30.0";  // string
        sheet.Cells[3, 0].Value = 40;      // number

        var filter = new SheetFilter(
            new InListCriterion { Column = 0, Values = [10, 30, "20"] },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // "10" matches 10 (numeric coercion)
        Assert.False(sheet.Rows.IsHidden(1)); // 20 matches "20" (numeric coercion)
        Assert.False(sheet.Rows.IsHidden(2)); // "30.0" matches 30 (numeric coercion)
        Assert.True(sheet.Rows.IsHidden(3));  // 40 - doesn't match
    }

    [Fact]
    public void Should_FilterWithInListCriterionForNullValues()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = null;
        sheet.Cells[2, 0].Value = "Cherry";

        var filter = new SheetFilter(
            new InListCriterion { Column = 0, Values = ["Apple", null, "Cherry"] },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // Apple - matches
        Assert.False(sheet.Rows.IsHidden(1)); // null - matches
        Assert.False(sheet.Rows.IsHidden(2)); // Cherry - matches
    }

    [Fact]
    public void Should_FilterWithInListCriterionForEmptyList()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = "Cherry";

        var filter = new SheetFilter(
            new InListCriterion { Column = 0, Values = [] },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // No matches in empty list
        Assert.True(sheet.Rows.IsHidden(1));  // No matches in empty list
        Assert.True(sheet.Rows.IsHidden(2));  // No matches in empty list
    }

    [Fact]
    public void Should_FilterWithInListCriterionInComplexFilter()
    {
        var sheet = new Sheet(6, 2);

        sheet.Cells[1, 0].Value = "Active"; sheet.Cells[1, 1].Value = 90;
        sheet.Cells[2, 0].Value = "Pending"; sheet.Cells[2, 1].Value = 45;
        sheet.Cells[3, 0].Value = "Inactive"; sheet.Cells[3, 1].Value = 95;
        sheet.Cells[4, 0].Value = "Suspended"; sheet.Cells[4, 1].Value = 100;
        sheet.Cells[5, 0].Value = "Active"; sheet.Cells[5, 1].Value = 30;

        var filter = new SheetFilter(
            new AndCriterion
            {
                Criteria = [
                    new InListCriterion {
                        Column = 0,
                        Values = ["Active", "Pending", "Suspended"]
                    },
                    new GreaterThanCriterion {
                        Column = 1, Value = 50
                    }
                ]
            },
            RangeRef.Parse("A2:B6")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(1)); // Active + 90 - matches both
        Assert.True(sheet.Rows.IsHidden(2));  // Pending + 45 - matches status but not value (should be hidden)
        Assert.True(sheet.Rows.IsHidden(3));  // Inactive + 95 - doesn't match status
        Assert.False(sheet.Rows.IsHidden(4)); // Suspended + 100 - matches both
        Assert.True(sheet.Rows.IsHidden(5));  // Active + 30 - matches status but not value
    }

    [Fact]
    public void Should_FilterWithIsNullCriterion()
    {
        sheet.Cells[0, 0].Value = null;
        sheet.Cells[1, 0].Value = "Apple";
        sheet.Cells[2, 0].Value = 10;
        sheet.Cells[3, 0].Value = null;

        var filter = new SheetFilter(
            new IsNullCriterion { Column = 0 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // null - matches
        Assert.True(sheet.Rows.IsHidden(1));  // "Apple" - doesn't match
        Assert.True(sheet.Rows.IsHidden(2));  // 10 - doesn't match
        Assert.False(sheet.Rows.IsHidden(3)); // null - matches
    }

    [Fact]
    public void Should_RemoveFilter()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );

        // Add filter
        sheet.AddFilter(filter);

        // Verify filter is applied
        Assert.Single(sheet.Filters);
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.True(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));

        // Remove filter
        var removed = sheet.RemoveFilter(filter);

        // Verify filter is removed
        Assert.True(removed);
        Assert.Empty(sheet.Filters);
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.False(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_RemoveFilterAt()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );

        // Add filter
        sheet.AddFilter(filter);

        // Verify filter is applied
        Assert.Single(sheet.Filters);
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.True(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));

        // Remove filter at index 0
        sheet.RemoveFilterAt(0);

        // Verify filter is removed
        Assert.Empty(sheet.Filters);
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.False(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_RemoveFilterReturnFalseForNonExistentFilter()
    {
        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );

        // Try to remove filter that was never added
        var removed = sheet.RemoveFilter(filter);

        // Should return false
        Assert.False(removed);
        Assert.Empty(sheet.Filters);
    }

    [Fact]
    public void Should_RemoveFilterAtThrowForInvalidIndex()
    {
        // Try to remove filter at invalid index
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.RemoveFilterAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.RemoveFilterAt(-1));
    }

    [Fact]
    public void Should_RemoveFilterAtThrowForNullFilter()
    {
        Assert.Throws<ArgumentNullException>(() => sheet.RemoveFilter(null!));
    }

    [Fact]
    public void Should_ApplyFiltersShowHiddenRowsWhenFilterRemoved()
    {
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";
        sheet.Cells[2, 0].Value = "A";

        var filter = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );

        // Add filter
        sheet.AddFilter(filter);

        // Verify rows are hidden
        Assert.True(sheet.Rows.IsHidden(1)); // "B" doesn't match

        // Remove filter
        sheet.RemoveFilter(filter);

        // Verify all rows are now visible
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.False(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_HandleMultipleFiltersWithRemove()
    {
        sheet.Cells[0, 0].Value = "A"; sheet.Cells[0, 1].Value = 10;
        sheet.Cells[1, 0].Value = "B"; sheet.Cells[1, 1].Value = 20;
        sheet.Cells[2, 0].Value = "A"; sheet.Cells[2, 1].Value = 30;

        var filter1 = new SheetFilter(
            new EqualToCriterion { Column = 0, Value = "A" },
            RangeRef.Parse("A1:A3")
        );

        var filter2 = new SheetFilter(
            new GreaterThanCriterion { Column = 1, Value = 15 },
            RangeRef.Parse("B1:B3")
        );

        // Add both filters
        sheet.AddFilter(filter1);
        sheet.AddFilter(filter2);

        // Verify both filters are applied
        Assert.Equal(2, sheet.Filters.Count);
        Assert.True(sheet.Rows.IsHidden(0));  // A=10 matches first filter but not second (10 <= 15)
        Assert.True(sheet.Rows.IsHidden(1));  // B=20 matches second filter but not first
        Assert.False(sheet.Rows.IsHidden(2)); // A=30 matches both filters

        // Remove first filter using the same instance
        var removed = sheet.RemoveFilter(filter1);
        Assert.True(removed);

        // Verify only second filter remains
        Assert.Single(sheet.Filters);
        Assert.True(sheet.Rows.IsHidden(0)); // A=10 doesn't match second filter (10 <= 15)
        Assert.False(sheet.Rows.IsHidden(1)); // B=20 matches second filter (20 > 15)
        Assert.False(sheet.Rows.IsHidden(2)); // A=30 matches second filter (30 > 15)

        // Remove second filter using the same instance
        removed = sheet.RemoveFilter(filter2);
        Assert.True(removed);

        // Verify no filters remain
        Assert.Empty(sheet.Filters);
        Assert.False(sheet.Rows.IsHidden(0));
        Assert.False(sheet.Rows.IsHidden(1));
        Assert.False(sheet.Rows.IsHidden(2));
    }

    [Fact]
    public void Should_FilterWithLessThanCriterion()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;
        sheet.Cells[2, 0].Value = 30;
        sheet.Cells[3, 0].Value = 40;

        var filter = new SheetFilter(
            new LessThanCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // 10 < 25 - matches
        Assert.False(sheet.Rows.IsHidden(1)); // 20 < 25 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // 30 >= 25 - doesn't match
        Assert.True(sheet.Rows.IsHidden(3));  // 40 >= 25 - doesn't match
    }

    [Fact]
    public void Should_FilterWithLessThanCriterionForMixedTypes()
    {
        sheet.Cells[0, 0].Value = "10";    // string
        sheet.Cells[1, 0].Value = 20;      // number
        sheet.Cells[2, 0].Value = "30.0";  // string
        sheet.Cells[3, 0].Value = 40;      // number

        var filter = new SheetFilter(
            new LessThanCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // "10" < 25 - matches
        Assert.False(sheet.Rows.IsHidden(1)); // 20 < 25 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // "30.0" >= 25 - doesn't match
        Assert.True(sheet.Rows.IsHidden(3));  // 40 >= 25 - doesn't match
    }

    [Fact]
    public void Should_FilterWithLessThanCriterionForNonNumericValues()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = 10;

        var filter = new SheetFilter(
            new LessThanCriterion { Column = 0, Value = 20 },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // "Apple" - not numeric, doesn't match
        Assert.True(sheet.Rows.IsHidden(1));  // "Banana" - not numeric, doesn't match
        Assert.False(sheet.Rows.IsHidden(2)); // 10 < 20 - matches
    }

    [Fact]
    public void Should_FilterWithGreaterThanOrEqualCriterion()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;
        sheet.Cells[2, 0].Value = 30;
        sheet.Cells[3, 0].Value = 40;

        var filter = new SheetFilter(
            new GreaterThanOrEqualCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // 10 < 25 - doesn't match
        Assert.True(sheet.Rows.IsHidden(1));  // 20 < 25 - doesn't match
        Assert.False(sheet.Rows.IsHidden(2)); // 30 >= 25 - matches
        Assert.False(sheet.Rows.IsHidden(3)); // 40 >= 25 - matches
    }

    [Fact]
    public void Should_FilterWithGreaterThanOrEqualCriterionForExactMatch()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 25;
        sheet.Cells[2, 0].Value = 30;

        var filter = new SheetFilter(
            new GreaterThanOrEqualCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // 10 < 25 - doesn't match
        Assert.False(sheet.Rows.IsHidden(1)); // 25 >= 25 - matches (exact)
        Assert.False(sheet.Rows.IsHidden(2)); // 30 >= 25 - matches
    }

    [Fact]
    public void Should_FilterWithLessThanOrEqualCriterion()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;
        sheet.Cells[2, 0].Value = 30;
        sheet.Cells[3, 0].Value = 40;

        var filter = new SheetFilter(
            new LessThanOrEqualCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // 10 <= 25 - matches
        Assert.False(sheet.Rows.IsHidden(1)); // 20 <= 25 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // 30 > 25 - doesn't match
        Assert.True(sheet.Rows.IsHidden(3));  // 40 > 25 - doesn't match
    }

    [Fact]
    public void Should_FilterWithLessThanOrEqualCriterionForExactMatch()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 25;
        sheet.Cells[2, 0].Value = 30;

        var filter = new SheetFilter(
            new LessThanOrEqualCriterion { Column = 0, Value = 25 },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // 10 <= 25 - matches
        Assert.False(sheet.Rows.IsHidden(1)); // 25 <= 25 - matches (exact)
        Assert.True(sheet.Rows.IsHidden(2));  // 30 > 25 - doesn't match
    }

    [Fact]
    public void Should_FilterWithNotEqualToCriterion()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = "Apple";
        sheet.Cells[3, 0].Value = "Cherry";

        var filter = new SheetFilter(
            new NotEqualToCriterion { Column = 0, Value = "Apple" },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // "Apple" == "Apple" - doesn't match
        Assert.False(sheet.Rows.IsHidden(1)); // "Banana" != "Apple" - matches
        Assert.True(sheet.Rows.IsHidden(2));  // "Apple" == "Apple" - doesn't match
        Assert.False(sheet.Rows.IsHidden(3)); // "Cherry" != "Apple" - matches
    }

    [Fact]
    public void Should_FilterWithNotEqualToCriterionForNumbers()
    {
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 20;
        sheet.Cells[2, 0].Value = 10;
        sheet.Cells[3, 0].Value = 30;

        var filter = new SheetFilter(
            new NotEqualToCriterion { Column = 0, Value = 10 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // 10 == 10 - doesn't match
        Assert.False(sheet.Rows.IsHidden(1)); // 20 != 10 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // 10 == 10 - doesn't match
        Assert.False(sheet.Rows.IsHidden(3)); // 30 != 10 - matches
    }

    [Fact]
    public void Should_FilterWithNotEqualToCriterionForMixedTypes()
    {
        sheet.Cells[0, 0].Value = "10";    // string
        sheet.Cells[1, 0].Value = 20;      // number
        sheet.Cells[2, 0].Value = "10.0";  // string
        sheet.Cells[3, 0].Value = 40;      // number

        var filter = new SheetFilter(
            new NotEqualToCriterion { Column = 0, Value = 10 },
            RangeRef.Parse("A1:A4")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // "10" == 10 (numeric coercion) - doesn't match
        Assert.False(sheet.Rows.IsHidden(1)); // 20 != 10 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // "10.0" == 10 (numeric coercion) - doesn't match
        Assert.False(sheet.Rows.IsHidden(3)); // 40 != 10 - matches
    }

    [Fact]
    public void Should_FilterWithNotEqualToCriterionForNonNumericStrings()
    {
        sheet.Cells[0, 0].Value = "Apple";
        sheet.Cells[1, 0].Value = "Banana";
        sheet.Cells[2, 0].Value = 10;

        var filter = new SheetFilter(
            new NotEqualToCriterion { Column = 0, Value = 10 },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(0)); // "Apple" != 10 - matches
        Assert.False(sheet.Rows.IsHidden(1)); // "Banana" != 10 - matches
        Assert.True(sheet.Rows.IsHidden(2));  // 10 == 10 - doesn't match
    }

    [Fact]
    public void Should_FilterWithNotEqualToCriterionForNullValues()
    {
        sheet.Cells[0, 0].Value = null;
        sheet.Cells[1, 0].Value = "Apple";
        sheet.Cells[2, 0].Value = null;

        var filter = new SheetFilter(
            new NotEqualToCriterion { Column = 0, Value = "Apple" },
            RangeRef.Parse("A1:A3")
        );
        sheet.AddFilter(filter);

        Assert.True(sheet.Rows.IsHidden(0));  // null != "Apple" but null handling returns false
        Assert.True(sheet.Rows.IsHidden(1));  // "Apple" == "Apple" - doesn't match
        Assert.True(sheet.Rows.IsHidden(2));  // null != "Apple" but null handling returns false
    }

    [Fact]
    public void Should_FilterWithComplexCombinationOfNewCriteria()
    {
        var sheet = new Sheet(6, 2);

        sheet.Cells[1, 0].Value = "Active"; sheet.Cells[1, 1].Value = 85;
        sheet.Cells[2, 0].Value = "Pending"; sheet.Cells[2, 1].Value = 60;
        sheet.Cells[3, 0].Value = "Inactive"; sheet.Cells[3, 1].Value = 90;
        sheet.Cells[4, 0].Value = "Suspended"; sheet.Cells[4, 1].Value = 45;
        sheet.Cells[5, 0].Value = "Active"; sheet.Cells[5, 1].Value = 95;

        var filter = new SheetFilter(
            new AndCriterion
            {
                Criteria = [
                    new OrCriterion {
                        Criteria = [
                            new EqualToCriterion { Column = 0, Value = "Active" },
                            new NotEqualToCriterion { Column = 0, Value = "Inactive" }
                        ]
                    },
                    new AndCriterion {
                        Criteria = [
                            new GreaterThanOrEqualCriterion { Column = 1, Value = 50 },
                            new LessThanOrEqualCriterion { Column = 1, Value = 90 }
                        ]
                    }
                ]
            },
            RangeRef.Parse("A2:B6")
        );
        sheet.AddFilter(filter);

        Assert.False(sheet.Rows.IsHidden(1)); // Active + 85 - matches both criteria
        Assert.False(sheet.Rows.IsHidden(2)); // Pending + 60 - matches both criteria
        Assert.True(sheet.Rows.IsHidden(3));  // Inactive + 90 - doesn't match first criterion
        Assert.True(sheet.Rows.IsHidden(4));  // Suspended + 45 - doesn't match second criterion
        Assert.True(sheet.Rows.IsHidden(5));  // Active + 95 - doesn't match second criterion
    }
}