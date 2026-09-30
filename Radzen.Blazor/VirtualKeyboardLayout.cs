using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen;

/// <summary>
/// Defines the keys of a <see cref="Radzen.Blazor.RadzenVirtualKeyboard" /> section as rows of key tokens.
/// A token is either a literal string inserted in the focused input (single characters, units of measure, plant-specific symbols)
/// or one of the special tokens <c>{backspace}</c>, <c>{enter}</c>, <c>{shift}</c>, <c>{space}</c>, <c>{tab}</c>,
/// <c>{clear}</c>, <c>{close}</c> and <c>{decimal}</c> (the culture-aware decimal separator).
/// </summary>
/// <example>
/// <code>
/// var layout = VirtualKeyboardLayout.FromRows("7 8 9", "4 5 6", "1 2 3", "0 {decimal} °C", "{clear} {backspace} {enter}");
/// </code>
/// </example>
public class VirtualKeyboardLayout
{
    /// <summary>
    /// Gets or sets the rows of key tokens.
    /// </summary>
    public IList<string[]> Rows { get; set; } = new List<string[]>();

    /// <summary>
    /// Gets or sets the rows of key tokens displayed while shift is active. Optional.
    /// Keys pair with <see cref="Rows" /> by position; special tokens keep their action regardless of the shift state.
    /// </summary>
    public IList<string[]>? ShiftRows { get; set; }

    /// <summary>
    /// Creates a layout from rows of space-separated key tokens.
    /// </summary>
    /// <param name="rows">The rows. Every row is a space-separated list of key tokens e.g. <c>"q w e r t y"</c>.</param>
    public static VirtualKeyboardLayout FromRows(params string[] rows)
    {
        return new VirtualKeyboardLayout
        {
            Rows = rows.Select(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList()
        };
    }

    /// <summary>
    /// Creates a layout with shift rows from rows of space-separated key tokens.
    /// </summary>
    /// <param name="rows">The rows displayed by default.</param>
    /// <param name="shiftRows">The rows displayed while shift is active.</param>
    public static VirtualKeyboardLayout FromRows(string[] rows, string[] shiftRows)
    {
        var layout = FromRows(rows);

        layout.ShiftRows = shiftRows.Select(row => row.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToList();

        return layout;
    }

    static readonly string[] qwertyRows =
    {
        "1 2 3 4 5 6 7 8 9 0",
        "q w e r t y u i o p",
        "a s d f g h j k l",
        "{shift} z x c v b n m {backspace}",
        "{clear} , {space} . {enter}"
    };

    static readonly string[] qwertyShiftRows =
    {
        "! @ # $ % ^ & * ( )",
        "Q W E R T Y U I O P",
        "A S D F G H J K L",
        "{shift} Z X C V B N M {backspace}",
        "{clear} ; {space} : {enter}"
    };

    static readonly string[] qwertzRows =
    {
        "1 2 3 4 5 6 7 8 9 0",
        "q w e r t z u i o p ü",
        "a s d f g h j k l ö ä",
        "{shift} y x c v b n m ß {backspace}",
        "{clear} , {space} . {enter}"
    };

    static readonly string[] qwertzShiftRows =
    {
        "! \" § $ % & / ( ) =",
        "Q W E R T Z U I O P Ü",
        "A S D F G H J K L Ö Ä",
        "{shift} Y X C V B N M ? {backspace}",
        "{clear} ; {space} : {enter}"
    };

    static readonly string[] azertyRows =
    {
        "1 2 3 4 5 6 7 8 9 0",
        "a z e r t y u i o p",
        "q s d f g h j k l m",
        "{shift} w x c v b n ' {backspace}",
        "{clear} é è ç à {space} {enter}"
    };

    static readonly string[] azertyShiftRows =
    {
        "& é \" ' ( - è _ ç à",
        "A Z E R T Y U I O P",
        "Q S D F G H J K L M",
        "{shift} W X C V B N ? {backspace}",
        "{clear} ! ; : / {space} {enter}"
    };

    static readonly string[] numpadRows =
    {
        "7 8 9",
        "4 5 6",
        "1 2 3",
        "- 0 {decimal}",
        "{clear} {backspace} {enter}"
    };

    /// <summary>
    /// Gets a QWERTY keyboard layout with a digit row.
    /// </summary>
    public static VirtualKeyboardLayout Qwerty => FromRows(qwertyRows, qwertyShiftRows);

    /// <summary>
    /// Gets a QWERTZ keyboard layout with a digit row.
    /// </summary>
    public static VirtualKeyboardLayout Qwertz => FromRows(qwertzRows, qwertzShiftRows);

    /// <summary>
    /// Gets an AZERTY keyboard layout with a digit row.
    /// </summary>
    public static VirtualKeyboardLayout Azerty => FromRows(azertyRows, azertyShiftRows);

    /// <summary>
    /// Gets a numeric keypad layout with a culture-aware decimal separator.
    /// </summary>
    public static VirtualKeyboardLayout Numpad => FromRows(numpadRows);
}
