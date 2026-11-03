using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor.Tests
{
    /// <summary>
    /// Keeps the bUnit 1 test surface (TestContext, RenderComponent, SetParametersAndRender, ToMarkup, DisposeComponents,
    /// ComponentParameter, IRenderedFragment) on top of bUnit 2 so tests written against either version compile unchanged.
    /// </summary>
    public class TestContext : BunitContext
    {
        public new IRenderedComponent<TComponent> RenderComponent<TComponent>() where TComponent : IComponent =>
            Render<TComponent>();

        public new IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder) where TComponent : IComponent =>
            Render<TComponent>(parameterBuilder);

        public IRenderedComponent<TComponent> RenderComponent<TComponent>(params ComponentParameter[] parameters) where TComponent : IComponent =>
            Render<TComponent>(builder =>
            {
                builder.OpenComponent<TComponent>(0);

                foreach (var parameter in parameters)
                {
                    builder.AddAttribute(1, parameter.Name, parameter.Value);
                }

                builder.CloseComponent();
            });

        public void DisposeComponents() =>
            DisposeComponentsAsync().GetAwaiter().GetResult();
    }

    public readonly struct ComponentParameter
    {
        public string Name { get; }

        public object Value { get; }

        ComponentParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public static ComponentParameter CreateParameter(string name, object value) => new ComponentParameter(name, value);
    }
}

namespace Bunit
{
    public static class BunitCompatibilityExtensions
    {
        public static void SetParametersAndRender<TComponent>(this IRenderedComponent<TComponent> component, Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder) where TComponent : IComponent =>
            component.Render(parameterBuilder);

        public static string ToMarkup(this IElement element) => element.OuterHtml;
    }
}
