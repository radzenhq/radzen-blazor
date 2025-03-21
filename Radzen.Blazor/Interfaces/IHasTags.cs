using System.Collections.Generic;

namespace Radzen.Blazor.Interfaces
{
	/// <summary>
	/// Interface representing an entity that has tags.
	/// </summary>
	internal interface IHasTags
	{
		/// <summary>
		/// Gets or sets the tags associated with the entity.
		/// </summary>
		public Dictionary<string, object> Tags { get; set; }
	}
}
