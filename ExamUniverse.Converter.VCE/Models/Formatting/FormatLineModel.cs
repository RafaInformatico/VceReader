using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.Formatting;

/// <summary>
///     Format line model
/// </summary>
public class FormatLineModel
{
    public FormatLineType Type { get; init; }
    public string Start { get; init; }
    public string End { get; init; }
}