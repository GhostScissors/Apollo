using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Apollo;

public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        if (x == null || y == null)
            return string.Compare(x, y, StringComparison.Ordinal);

        var regex = new Regex(@"\d+|\D+");

        var xMatches = regex.Matches(x);
        var yMatches = regex.Matches(y);

        for (int i = 0; i < xMatches.Count && i < yMatches.Count; i++)
        {
            var xPart = xMatches[i].Value;
            var yPart = yMatches[i].Value;

            if (int.TryParse(xPart, out int xNum) && int.TryParse(yPart, out int yNum))
            {
                // Compare numbers numerically
                int result = xNum.CompareTo(yNum);
                if (result != 0)
                    return result;
            }
            else
            {
                // Compare non-numeric parts lexicographically
                int result = string.Compare(xPart, yPart, StringComparison.Ordinal);
                if (result != 0)
                    return result;
            }
        }

        // If all parts are equal, compare by length
        return xMatches.Count.CompareTo(yMatches.Count);
    }
}