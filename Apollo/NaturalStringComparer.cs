using System.Text.RegularExpressions;

namespace Apollo;

public partial class NaturalStringComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
            return string.Compare(x, y, StringComparison.Ordinal);

        var regex = MyRegex();

        var xMatches = regex.Matches(x);
        var yMatches = regex.Matches(y);

        for (var i = 0; i < xMatches.Count && i < yMatches.Count; i++)
        {
            var xPart = xMatches[i].Value;
            var yPart = yMatches[i].Value;

            if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
            {
                var result = xNum.CompareTo(yNum);
                if (result != 0)
                    return result;
            }
            else
            {
                var result = string.Compare(xPart, yPart, StringComparison.Ordinal);
                if (result != 0)
                    return result;
            }
        }

        return xMatches.Count.CompareTo(yMatches.Count);
    }

    [GeneratedRegex(@"\d+|\D+")]
    private static partial Regex MyRegex();
}