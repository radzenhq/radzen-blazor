#nullable enable

namespace Radzen.Documents.Spreadsheet;

abstract class IsFunctionBase : FormulaFunction
{
    public override bool CanHandleErrors => true;

    public override FunctionParameter[] Parameters =>
    [
        new("value", ParameterType.Single, isRequired: true)
    ];

    protected abstract bool Test(CellData value);

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var value = arguments.GetSingle("value");

        if (value is null)
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromBoolean(Test(value));
    }
}

class IsBlankFunction : IsFunctionBase
{
    public override string Name => "ISBLANK";

    // Only a truly empty cell is blank; a formula-produced "" is not.
    protected override bool Test(CellData value) => value.IsEmpty;
}

class IsNumberFunction : IsFunctionBase
{
    public override string Name => "ISNUMBER";

    protected override bool Test(CellData value) => value.Type is CellDataType.Number or CellDataType.Date;
}

class IsTextFunction : IsFunctionBase
{
    public override string Name => "ISTEXT";

    protected override bool Test(CellData value) => value.Type == CellDataType.String;
}

class IsErrorFunction : IsFunctionBase
{
    public override string Name => "ISERROR";

    protected override bool Test(CellData value) => value.IsError;
}

class IsNaFunction : IsFunctionBase
{
    public override string Name => "ISNA";

    protected override bool Test(CellData value) =>
        value.IsError && value.GetValueOrDefault<CellError>() == CellError.NA;
}
