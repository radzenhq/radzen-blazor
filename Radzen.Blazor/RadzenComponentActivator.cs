using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Radzen
{
    /// <summary>
    ///  Allows the developer to replace a component with another. Useful to specify default values for component properties.
    /// </summary>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// var activator = new RadzenComponentActivator();
    /// // Replace RadzenButton with MyButton
    /// activator.Override&lt;RadzenButton, MyButton&gt;();
    /// // Replace RadzenDataGrid with MyDataGrid
    /// activator.Override(typeof(RadzenDataGrid&lt;&gt;), typeof(MyDataGrid&lt;&gt;));
    /// // Register the activator
    /// builder.Services.AddSingleton&lt;IComponentActivator&gt;(activator);
    /// </code>
    /// </example>
    public class RadzenComponentActivator : IComponentActivator
    {
        private readonly Dictionary<Type, Type> replacedTypes = new Dictionary<Type, Type>();

        /// <summary>
        ///  Replaces the specified component type with another.
        /// </summary>
        /// <typeparam name="TOriginal"></typeparam>
        /// <typeparam name="TOverride"></typeparam>
        public void Override<TOriginal, TOverride>()
        {
            Override(typeof(TOriginal), typeof(TOverride));
        }

        /// <summary>
        /// Replaces the specified component type with another.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="override"></param>
        public void Override(Type original, Type @override)
        {
            replacedTypes.Add(original, @override);
        }

        /// <summary>
        ///   Creates a component of the specified type.
        /// </summary>
        /// <param name="componentType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IComponent CreateInstance(Type componentType)
        {
            ArgumentNullException.ThrowIfNull(componentType);

            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", nameof(componentType));
            }

            if (replacedTypes.TryGetValue(componentType, out var replacedType))
            {
                componentType = replacedType;
            }
            else if (componentType.IsGenericType)
            {
                var genericTypeDefinition = componentType.GetGenericTypeDefinition();

                if (replacedTypes.TryGetValue(genericTypeDefinition, out var replacedGenericType))
                {
                    componentType = replacedGenericType.MakeGenericType(componentType.GetGenericArguments());
                }
            }

        var instance = Activator.CreateInstance(componentType)
            ?? throw new InvalidOperationException($"Unable to create an instance of '{componentType.FullName}'.");

        return (IComponent)instance;
        }
    }
}