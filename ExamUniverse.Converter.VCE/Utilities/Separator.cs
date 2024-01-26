using System;
using System.Collections.Generic;
using System.Linq;

namespace ExamUniverse.Converter.VCE.Utilities;

/// <summary>
///     Separator
/// </summary>
public class Separator
{
    private byte[] _bytes;
    private readonly byte[][] _splitters;

    public Separator(byte[] data, byte[] splitter)
    {
        _bytes = data;
        _splitters = [splitter];
    }

    public Separator(byte[] data, byte[][] splitters)
    {
        _bytes = data;
        _splitters = splitters;
    }

    /// <summary>
    ///     Has value
    /// </summary>
    public bool HasValue => _bytes.Length > 0;

    /// <summary>
    ///     Read int 32
    /// </summary>
    /// <returns></returns>
    public int ReadInt32()
    {
        var bytes = ReadBytes(4);
        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>
    ///     Read bytes
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public byte[] ReadBytes(int length)
    {
        var bytes = _bytes.Take(length).ToArray();
        _bytes = _bytes.Skip(bytes.Length).ToArray();

        return bytes;
    }

    /// <summary>
    ///     Push
    /// </summary>
    /// <param name="bytes"></param>
    public void Push(IEnumerable<byte> bytes)
    {
        var data = new List<byte>();

        data.AddRange(_bytes);
        data.AddRange(bytes);

        _bytes = data.ToArray();
    }

    /// <summary>
    ///     Pop
    /// </summary>
    /// <returns></returns>
    public byte[] Pop()
    {
        var data = GetPatternBytes();
        _bytes = _bytes.Skip(data.Length).ToArray();

        return data;
    }

    /// <summary>
    ///     Peek
    /// </summary>
    /// <returns></returns>
    public byte[] Peek()
    {
        return GetPatternBytes();
    }

    /// <summary>
    ///     Get pattern bytes
    /// </summary>
    /// <returns></returns>
    private byte[] GetPatternBytes()
    {
        for (var i = 0; i < _bytes.Length; i++)
        {
            foreach (var t in _splitters)
            {
                if (!IsMatch(_bytes, i, t))
                {
                    continue;
                }

                var count = i == 0 ? t.Length : i;
                return _bytes.Take(count).ToArray();
            }
        }

        return _bytes.Take(_bytes.Length).ToArray();
    }

    /// <summary>
    ///     Is match
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="position"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    private static bool IsMatch(IReadOnlyList<byte> bytes, int position, IReadOnlyCollection<byte> pattern)
    {
        if (pattern.Count > bytes.Count - position)
        {
            return false;
        }

        return !pattern.Where((t, i) => bytes[position + i] != t).Any();
    }
}