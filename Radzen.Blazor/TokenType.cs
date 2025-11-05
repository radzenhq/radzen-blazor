namespace Radzen;

/// <summary>
/// Represents the type of token in an expression.
/// </summary>
internal enum TokenType
{
    /// <summary>
    /// No token type.
    /// </summary>
    None,

    /// <summary>
    /// Identifier token.
    /// </summary>
    Identifier,

    /// <summary>
    /// Equals equals token (==).
    /// </summary>
    EqualsEquals,

    /// <summary>
    /// Not equals token (!=).
    /// </summary>
    NotEquals,

    /// <summary>
    /// Equals greater than token (=>).
    /// </summary>
    EqualsGreaterThan,

    /// <summary>
    /// String literal token.
    /// </summary>
    StringLiteral,

    /// <summary>
    /// Numeric literal token.
    /// </summary>
    NumericLiteral,

    /// <summary>
    /// Dot token (.).
    /// </summary>
    Dot,

    /// <summary>
    /// Open parenthesis token.
    /// </summary>
    OpenParen,

    /// <summary>
    /// Close parenthesis token.
    /// </summary>
    CloseParen,

    /// <summary>
    /// Comma token.
    /// </summary>
    Comma,

    /// <summary>
    /// Ampersand ampersand token (&amp;&amp;).
    /// </summary>
    AmpersandAmpersand,

    /// <summary>
    /// Ampersand token (&amp;).
    /// </summary>
    Ampersand,

    /// <summary>
    /// Bar bar token (||).
    /// </summary>
    BarBar,

    /// <summary>
    /// Bar token (|).
    /// </summary>
    Bar,

    /// <summary>
    /// Greater than token (&gt;).
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Less than token (&lt;).
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal token (&lt;=).
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Greater than or equal token (&gt;=).
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Plus token (+).
    /// </summary>
    Plus,

    /// <summary>
    /// Minus token (-).
    /// </summary>
    Minus,

    /// <summary>
    /// Star token (*).
    /// </summary>
    Star,

    /// <summary>
    /// Slash token (/).
    /// </summary>
    Slash,

    /// <summary>
    /// Character literal token.
    /// </summary>
    CharacterLiteral,

    /// <summary>
    /// Question mark token (?).
    /// </summary>
    QuestionMark,

    /// <summary>
    /// Question mark question mark token (??).
    /// </summary>
    QuestionMarkQuestionMark,

    /// <summary>
    /// Colon token (:).
    /// </summary>
    Colon,

    /// <summary>
    /// Question dot token (?.).
    /// </summary>
    QuestionDot,

    /// <summary>
    /// New keyword token.
    /// </summary>
    New,

    /// <summary>
    /// Null literal token.
    /// </summary>
    NullLiteral,

    /// <summary>
    /// True literal token.
    /// </summary>
    TrueLiteral,

    /// <summary>
    /// False literal token.
    /// </summary>
    FalseLiteral,

    /// <summary>
    /// Open bracket token ([).
    /// </summary>
    OpenBracket,

    /// <summary>
    /// Close bracket token (]).
    /// </summary>
    CloseBracket,

    /// <summary>
    /// Open brace token ({).
    /// </summary>
    OpenBrace,

    /// <summary>
    /// Close brace token (}).
    /// </summary>
    CloseBrace,

    /// <summary>
    /// Exclamation mark token (!).
    /// </summary>
    ExclamationMark,

    /// <summary>
    /// Equals token (=).
    /// </summary>
    Equals,

    /// <summary>
    /// Caret token (^).
    /// </summary>
    Caret,

    /// <summary>
    /// Greater than greater than token (&gt;&gt;).
    /// </summary>
    GreaterThanGreaterThan,

    /// <summary>
    /// Less than less than token (&lt;&lt;).
    /// </summary>
    LessThanLessThan,
}

