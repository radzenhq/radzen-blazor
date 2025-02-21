using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface that has to be implemented by a resource in order to by supported by <see cref="RadzenScheduler{TItem}" />.
    /// </summary>
    public interface ISchedulerResource
    {
        /// <summary>
        /// Gets the title of the resource.
        /// </summary>
        /// <value>The title.</value>
        string Title { get; set; }
        /// <summary>
        /// Gets the property of the appointment data that corespondes to this resource
        /// </summary>
        /// <value>The property.</value>
        string Property { get; set; }
        /// <summary>
        /// Gets the order of grouping
        /// </summary>
        /// <value>The order.</value>
        int Order { get; set; }
        /// <summary>
        /// Gets whether the resource is active in grouping
        /// </summary>
        /// <value>The visibility.</value>
        bool IsActive { get; set; }
        /// <summary>
        /// Gets the items of the resource.
        /// </summary>
        /// <value>The data items.</value>
        IEnumerable<string> Data { get; set; }
    }
}