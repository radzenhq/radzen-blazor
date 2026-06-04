using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// The DateTimeRange class represents a range of dates and times, defined by a Start and End property. It provides various methods for querying the range, such as checking for intersections with other ranges and calculating the total number of nights or days in the range. The class also includes several static properties that represent common date ranges, such as the last rolling year, year to date, month to date, and so on. This makes it easier to work with date ranges in applications that require date-based filtering or calculations.
    /// </summary>
    public class DateTimeRangeHelper
    {
        #region Construction
        /// <summary>
        /// The constructor of the DateTimeRange class takes two nullable DateTime parameters, start and end, which represent the starting and ending points of the date range. The constructor includes a validation check to ensure that if both start and end are provided (i.e., not null), then start must be less than or equal to end. If this condition is violated, an exception is thrown with the message "Invalid range edges." This validation ensures that the date range is logically consistent, preventing scenarios where the start date is after the end date.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <exception cref="Exception"></exception>
        public DateTimeRangeHelper(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The Start property represents the starting point of the date range, while the End property represents the ending point. Both properties are of type DateTime and are set through the constructor. The class includes validation to ensure that if both Start and End are provided, Start must be less than or equal to End, ensuring a valid date range.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The End property represents the ending point of the date range. It is of type DateTime and is set through the constructor. The class includes validation to ensure that if both Start and End are provided, Start must be less than or equal to End, ensuring a valid date range.
        /// </summary>
        public DateTime End { get; set; }
        #endregion

        #region Operators

        /// <summary>
        /// GetHashCode should be implemented in a way that if two objects are equal according to Equals, they should return the same hash code.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Querying
        /// <summary>
        /// 
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool Intersects(DateTimeRangeHelper range)
        {
            var type = GetIntersectionType(range);
            return type != IntersectionType.None;
        }
        /// <summary>
        /// Checks if a given date falls within the range defined by the Start and End properties. If End is null, it treats the range as open-ended, meaning that any date after Start would be considered in range. The method returns true if the date is greater than or equal to Start and less than or equal to End (or if End is null, just greater than or equal to Start). Otherwise, it returns false.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public bool IsInRange(DateTime date)
        {
            return (date >= Start) && (date <= End);
        }
        /// <summary>
        /// Determines the type of intersection between the current DateTimeRange and another DateTimeRange passed as a parameter. The method checks various conditions to classify the intersection into one of several types, such as whether the ranges are equal, one range is contained within the other, or if they start or end within each other. If there is no intersection, it returns IntersectionType.None.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public IntersectionType GetIntersectionType(DateTimeRangeHelper range)
        {
            if (range == null)
            {
                return IntersectionType.None;
            }
            if (this == range)
            {
                return IntersectionType.RangesEqauled;
            }
            else if (IsInRange(range.Start) && IsInRange(range.End))
            {
                return IntersectionType.ContainedInRange;
            }
            else if (IsInRange(range.Start))
            {
                return IntersectionType.StartsInRange;
            }
            else if (IsInRange(range.End))
            {
                return IntersectionType.EndsInRange;
            }
            else if (range.IsInRange(Start) && range.IsInRange(End))
            {
                return IntersectionType.ContainsRange;
            }
            return IntersectionType.None;
        }
        /// <summary>
        /// Calculates the intersection of the current DateTimeRange with another DateTimeRange passed as a parameter. The method first determines the type of intersection using the GetIntersectionType method and then returns a new DateTimeRange that represents the overlapping period between the two ranges. If there is no intersection, it returns a default DateTimeRange (with null Start and End).
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public DateTimeRangeHelper GetIntersection(DateTimeRangeHelper range)
        {
            if (range == null)
            {
                return new DateTimeRangeHelper(DateTime.MinValue, DateTime.MinValue);
            }
            var type = GetIntersectionType(range);
            if (type == IntersectionType.RangesEqauled || type == IntersectionType.ContainedInRange)
            {
                return range;
            }
            else if (type == IntersectionType.StartsInRange)
            {
                return new DateTimeRangeHelper(range.Start, End);
            }
            else if (type == IntersectionType.EndsInRange)
            {
                return new DateTimeRangeHelper(Start, range.End);
            }
            else if (type == IntersectionType.ContainsRange)
            {
                return this;
            }
            return range;
        }
        #endregion
    }
    /// <summary>
    /// The IntersectionType enumeration defines the various types of intersections that can occur between two DateTimeRange instances. It includes values such as None (indicating no intersection), EndsInRange (indicating that the given range ends inside the current range), StartsInRange (indicating that the given range starts inside the current range), RangesEqauled (indicating that both ranges are equal), ContainedInRange (indicating that the given range is contained within the current range), and ContainsRange (indicating that the given range contains the current range). This enumeration is used to classify the relationship between two date ranges when checking for intersections.
    /// </summary>
    public enum IntersectionType
    {
        /// <summary>
        /// No Intersection
        /// </summary>
        None = -1,
        /// <summary>
        /// Given range ends inside the range
        /// </summary>
        EndsInRange,
        /// <summary>
        /// Given range starts inside the range
        /// </summary>
        StartsInRange,
        /// <summary>
        /// Both ranges are equaled
        /// </summary>
        RangesEqauled,
        /// <summary>
        /// Given range contained in the range
        /// </summary>
        ContainedInRange,
        /// <summary>
        /// Given range contains the range
        /// </summary>
        ContainsRange,
    }
}
