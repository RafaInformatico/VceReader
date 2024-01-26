using System.Collections.Generic;
using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.FileReader;

/// <summary>
///     Exam question model
/// </summary>
public class ExamQuestionModel
{
    public int Id { get; set; }
    public int ExamSectionId { get; set; }

    public ExamQuestionType Type { get; set; }

    public int SectionId { get; set; }
    public int Complexity { get; set; }

    public string? Question { get; set; }

    public int VariantsCount { get; set; }
    public List<string> Variants { get; } = [];

    public string? AreaImage { get; set; }

    public int HotAreasCount { get; set; }
    public List<HotAreaModel> HotAreas { get; } = [];

    public List<byte> DragAndDropTypes { get; } = [];

    public int DragAreasCount { get; set; }
    public List<DragAndDropAreaModel> DragAreas { get; } = [];

    public int DropAreasCount { get; set; }
    public List<DragAndDropAreaModel> DropAreas { get; } = [];

    public string? Reference { get; set; }
    public string? Answers { get; set; }

    public int BlockAnswersCount { get; set; }
    public List<string?> BlockAnswers { get; set; } = [];
}