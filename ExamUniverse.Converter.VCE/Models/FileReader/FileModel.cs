using System.Collections.Generic;

namespace ExamUniverse.Converter.VCE.Models.FileReader;

/// <summary>
///     File model
/// </summary>
public class FileModel
{
    public int Version { get; set; }

    public byte[] Keys { get; set; }
    public byte[] EncryptKeys { get; set; }
    public byte[] DecryptKeys { get; init; }

    public string Number { get; set; }
    public string Title { get; set; }

    public int PassingScore { get; set; }
    public int TimeLimit { get; set; }

    public string FileVersion { get; set; }

    public int SectionsCount { get; set; }
    public List<SectionModel> Sections { get; } = new();

    public int ExamsCount { get; set; }
    public List<ExamModel> Exams { get; } = new();
}