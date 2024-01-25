using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.FileReader;

/// <summary>
///     Exam section model
/// </summary>
public class ExamSectionModel
{
    public int Id { get; init; }

    public ExamSectionType Type { get; init; }
    public int TimeLimit { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
}