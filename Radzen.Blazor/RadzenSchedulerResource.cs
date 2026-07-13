using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Defines a resource type (rooms, people, equipment) of <see cref="RadzenScheduler{TItem}" />. Declare one instance per resource type as child content of the scheduler.
    /// Views group appointments by the declared resource types when their <see cref="SchedulerViewBase.GroupByResource" /> property is set to <c>true</c>.
    /// The order of declaration determines the grouping hierarchy - the first resource type is the top level.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenScheduler Data="@bookings" StartProperty="Start" EndProperty="End" TextProperty="Text"&gt;
    ///     &lt;RadzenSchedulerResource Name="Room" Data="@rooms" Property="RoomId" TextProperty="Name" ValueProperty="Id" /&gt;
    ///     &lt;RadzenSchedulerResource Name="Employee" Data="@employees" Property="EmployeeIds" TextProperty="FullName" ValueProperty="Id" /&gt;
    ///     &lt;RadzenDayView GroupByResource="true" /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public class RadzenSchedulerResource : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the scheduler instance.
        /// </summary>
        [CascadingParameter]
        public IScheduler? Scheduler { get; set; }

        private string? name;

        /// <summary>
        /// Gets or sets the name of the resource type. Used as key in <see cref="SchedulerSlotSelectEventArgs.Resources" /> and related event arguments.
        /// Set to the value of <see cref="Property" /> by default.
        /// </summary>
        [Parameter]
        public string Name
        {
            get => name ?? Property ?? string.Empty;
            set => name = value;
        }

        /// <summary>
        /// Gets or sets the collection of resource items of this type.
        /// </summary>
        [Parameter]
        public IEnumerable? Data { get; set; }

        /// <summary>
        /// Specifies the property of an appointment data item which contains the value of the resource it belongs to.
        /// The property value is compared to the value of <see cref="ValueProperty" /> of every resource item. Collection properties are
        /// supported - the appointment is displayed for every resource item whose value the collection contains.
        /// </summary>
        [Parameter]
        public string? Property { get; set; }

        /// <summary>
        /// Specifies the property of a resource item which contains its display text. The resource item itself is used when not set.
        /// </summary>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// Specifies the property of a resource item which contains its unique value. The resource item itself is used when not set.
        /// </summary>
        [Parameter]
        public string? ValueProperty { get; set; }

        /// <summary>
        /// Gets or sets the template used to render the headers of this resource type in views which group appointments by resource. The resource item is passed as context.
        /// </summary>
        [Parameter]
        public RenderFragment<object>? HeaderTemplate { get; set; }

        /// <summary>
        /// Gets the resource items as a list.
        /// </summary>
        public IList<object> Items => items ??= Data?.Cast<object>().ToList() ?? new List<object>();

        private IList<object>? items;

        /// <summary>
        /// Gets the display text of the specified resource item.
        /// </summary>
        /// <param name="item">The resource item.</param>
        /// <returns>The display text.</returns>
        public string GetText(object item)
        {
            var value = string.IsNullOrEmpty(TextProperty) ? item : PropertyAccess.GetItemOrValueFromProperty(item, TextProperty);

            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the specified appointment belongs to the specified resource item of this type.
        /// </summary>
        /// <param name="appointment">The appointment to check.</param>
        /// <param name="item">The resource item.</param>
        /// <returns><c>true</c> if the appointment belongs to the resource item; otherwise, <c>false</c>.</returns>
        public bool IsAppointmentInResource(AppointmentData appointment, object item)
        {
            if (appointment == null || item == null || string.IsNullOrEmpty(Property))
            {
                return false;
            }

            var value = PropertyAccess.GetItemOrValueFromProperty(appointment.Data, Property);
            var itemValue = string.IsNullOrEmpty(ValueProperty) ? item : PropertyAccess.GetItemOrValueFromProperty(item, ValueProperty);

            if (value is IEnumerable enumerable && value is not string)
            {
                return enumerable.Cast<object>().Any(entry => object.Equals(entry, itemValue));
            }

            return object.Equals(value, itemValue);
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Data), Data))
            {
                items = null;
            }

            await base.SetParametersAsync(parameters);

            if (Scheduler != null)
            {
                await Scheduler.AddResource(this);
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Scheduler?.RemoveResource(this);
            GC.SuppressFinalize(this);
        }
    }
}
