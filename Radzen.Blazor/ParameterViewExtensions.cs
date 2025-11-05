using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Radzen;

/// <summary>
/// Contains extension methods for <see cref="ParameterView" />.
/// </summary>
public static class ParameterViewExtensions
{
    /// <summary>
    /// Checks if a parameter changed.
    /// </summary>
    /// <typeparam name="T">The value type</typeparam>
    /// <param name="parameters">The parameters.</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="parameterValue">The parameter value.</param>
    /// <returns><c>true</c> if the parameter value has changed, <c>false</c> otherwise.</returns>
    public static bool DidParameterChange<T>(this ParameterView parameters, string parameterName, T parameterValue)
    {
        if (parameters.TryGetValue(parameterName, out T value))
        {
            return !EqualityComparer<T>.Default.Equals(value, parameterValue);
        }

        return false;
    }
}

