using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Defines a custom cell type with optional renderer and editor components for a spreadsheet.
/// </summary>
public class SpreadsheetCellType
{
    /// <summary>
    /// Gets or sets the component type used to render the cell.
    /// The component must accept a <see cref="SpreadsheetCellRenderContext"/> parameter named <c>Context</c>.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? RendererType { get; set; }

    /// <summary>
    /// Gets or sets the component type used to edit the cell.
    /// The component must accept a <see cref="SpreadsheetCellEditContext"/> parameter named <c>Context</c>.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? EditorType { get; set; }

    /// <summary>
    /// Creates a <see cref="SpreadsheetCellType"/> with a custom renderer.
    /// </summary>
    public static SpreadsheetCellType Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRenderer>() where TRenderer : IComponent
        => new() { RendererType = typeof(TRenderer) };

    /// <summary>
    /// Creates a <see cref="SpreadsheetCellType"/> with a custom renderer and editor.
    /// </summary>
    public static SpreadsheetCellType Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TRenderer, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEditor>()
        where TRenderer : IComponent
        where TEditor : IComponent
        => new() { RendererType = typeof(TRenderer), EditorType = typeof(TEditor) };
}
