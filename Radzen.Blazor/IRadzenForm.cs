using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Represents the common <see cref="RadzenTemplateForm{TItem}" /> API used by
/// its items. Injected as a cascading property in <see cref="IRadzenFormComponent" />.
/// </summary>
public interface IRadzenForm
{
    /// <summary>
    /// Adds the specified component to the form.
    /// </summary>
    /// <param name="component">The component to add to the form.</param>
    void AddComponent(IRadzenFormComponent component);

    /// <summary>
    /// Removes the component from the form.
    /// </summary>
    /// <param name="component">The component to remove from the form.</param>
    void RemoveComponent(IRadzenFormComponent component);

    /// <summary>
    /// Finds a form component by its name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The component whose <see cref="IRadzenFormComponent.Name" /> equals to <paramref name="name" />; <c>null</c> if such a component is not found.</returns>
    IRadzenFormComponent FindComponent(string name);
}

