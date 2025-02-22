﻿using System.Collections.Generic;
using ExamUniverse.Converter.VCE.Enums;

namespace ExamUniverse.Converter.VCE.Models.FileReader;

/// <summary>
///     Exam model
/// </summary>
public class ExamModel
{
    public int Id { get; set; }

    public ExamType Type { get; init; }
    public string? Name { get; init; }

    public int ExamSectionsCount { get; set; }
    public List<ExamSectionModel> ExamSections { get; } = [];

    public int ExamQuestionsCount { get; set; }
    public List<ExamQuestionModel> ExamQuestions { get; } = [];
}