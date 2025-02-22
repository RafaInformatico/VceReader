﻿namespace ExamUniverse.Converter.VCE.Extensions;

/// <summary>
///     String extension
/// </summary>
public static class StringExtension
{
    /// <summary>
    ///     Trim start
    /// </summary>
    /// <param name="target"></param>
    /// <param name="trimString"></param>
    /// <returns></returns>
    public static string TrimStart(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString)) return target;

        var result = target;

        while (result.StartsWith(trimString)) result = result[trimString.Length..];

        return result;
    }

    /// <summary>
    ///     Trim end
    /// </summary>
    /// <param name="target"></param>
    /// <param name="trimString"></param>
    /// <returns></returns>
    public static string TrimEnd(this string target, string trimString)
    {
        if (string.IsNullOrEmpty(trimString)) return target;

        var result = target;

        while (result.EndsWith(trimString)) result = result[..^trimString.Length];

        return result;
    }
}