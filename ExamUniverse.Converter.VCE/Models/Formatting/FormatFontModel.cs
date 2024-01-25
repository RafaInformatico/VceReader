using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.Formatting;

/// <summary>
///     Format font model
/// </summary>
public class FormatFontModel
{
    public FormatFontType Type { get; init; }
    public string Start { get; init; }
    public string End { get; init; }
}