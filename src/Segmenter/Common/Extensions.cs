using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace JiebaNet.Segmenter.Common;

public static partial class Extensions
{
    [GeneratedRegex(@"\d+")]
    private static partial Regex RegexDigits();
    
    #region Enumerable

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty<T>(this IEnumerable<T>? enumerable)
    {
        return (enumerable == null) || !enumerable.Any();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNotEmpty<T>(this IEnumerable<T>? enumerable)
    {
        return (enumerable != null) && enumerable.Any();
    }

    public static TValue? GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> d, TKey key) where TKey : notnull
    {
        return d.TryGetValue(key, out var value) ? value : default;
    }

    public static void Update<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
    {
        foreach (var key in other.Keys)
        {
            dict[key] = other[key];
        }
    }

    #endregion

    #region String & Text

    public static string Left(this string s, int endIndex)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        return s.Substring(0, endIndex);
    }

    public static string Right(this string s, int startIndex)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }


        return s.Substring(startIndex);
    }

    public static string Sub(this string s, int startIndex, int endIndex)
    {
        return s.Substring(startIndex, endIndex - startIndex);
    }

    public static bool IsInt32(this string s)
    {
        return RegexDigits().IsMatch(s);
    }
        
    public static string[] SplitLines(this string s)
    {
        return s.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    public static string Join(this IEnumerable<string> inputs, string separator = ", ")
    {
        return string.Join(separator, inputs);
    }

    public static IEnumerable<string> SubGroupValues(this GroupCollection groups)
    {
        var result = from Group g in groups
            select g.Value;
        return result.Skip(1);
    }

    #endregion
}