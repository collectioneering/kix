using System.CommandLine;
using System.Text;

namespace Art.Tesler;

public static class CommandHelper
{
    public static string GetOptionAlias(Option option)
    {
        if (option.Aliases.Count > 0)
        {
            return new StringBuilder().AppendJoin('/', option.Aliases).ToString();
        }
        return option.Name;
    }

    public static string GetOptionAliasList(IEnumerable<Option> options, string separator = ", ")
    {
        StringBuilder stringBuilder = new();
        bool first = true;
        foreach (var option in options)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                stringBuilder.Append(separator);
            }
            if (option.Aliases.Count > 0)
            {
                stringBuilder.AppendJoin('/', option.Aliases);
            }
            else
            {
                stringBuilder.Append(option.Name);
            }
        }
        return stringBuilder.ToString();
    }
}
