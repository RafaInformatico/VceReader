﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExamUniverse.Converter.VCE.Extensions;

/// <summary>
///     Array extension
/// </summary>
public static class ArrayExtension
{
    /// <summary>
    ///     Get string
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string GetString(this IEnumerable<byte>? bytes)
    {
        return Encoding.UTF8.GetString((bytes ?? Array.Empty<byte>()).Where(b => !char.IsControl((char)b)).ToArray());
    }

    /// <summary>
    ///     Get string with replaced
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string GetStringWithReplaced(this IEnumerable<byte>? bytes)
    {
        return bytes.GetString().Replace("\0", "");
    }
}