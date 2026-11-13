namespace Radzen.Documents.Markdown;

#nullable enable

static class NestingLimit
{
    public const int MaxDepth = 100;

    public static bool IsReached(int depth)
    {
        return depth >= MaxDepth;
    }

    public static bool IsReached(Block container)
    {
        var depth = 0;

        for (var block = container; block is not null and not Document; block = block.Parent)
        {
            if (IsReached(++depth))
            {
                return true;
            }
        }

        return false;
    }
}
