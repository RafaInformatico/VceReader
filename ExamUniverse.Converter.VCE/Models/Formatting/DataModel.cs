using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.Formatting;

/// <summary>
///     Data model
/// </summary>
public class DataModel
{
    public byte[] Data { get; init; }
    public DataType Type { get; init; }

    public byte[] Property1 { get; init; }
    public byte[] Property2 { get; init; }
}