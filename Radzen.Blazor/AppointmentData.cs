using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents an appointment in <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    public class AppointmentData
    {
        /// <summary>
        /// Gets or sets the start of the appointment.
        /// </summary>
        /// <value>The start.</value>
        public DateTime Start { get; set; }
        /// <summary>
        /// Gets or sets the end of the appointment.
        /// </summary>
        /// <value>The end.</value>
        public DateTime End { get; set; }
        /// <summary>
        /// Gets or sets the text of the appointment.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the data associated with the appointment
        /// </summary>
        /// <value>The data.</value>
        public object Data { get; set; }

        /// <summary>
        /// Determines whether the specified object is equal to this instance. Used to check if two appointments are equal.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><c>true</c> if the specified is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is AppointmentData data &&
                   Start == data.Start &&
                   End == data.End &&
                   Text == data.Text &&
                   EqualityComparer<object>.Default.Equals(Data, data.Data);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End, Text, Data);
        }
    }
}