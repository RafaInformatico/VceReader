using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExamUniverse.Converter.VCE.Enums;
using ExamUniverse.Converter.VCE.Extensions;
using ExamUniverse.Converter.VCE.Models.FileReader;
using ExamUniverse.Converter.VCE.Models.Formatting;
using ExamUniverse.Converter.VCE.Services.Interfaces;
using ExamUniverse.Converter.VCE.Utilities;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace ExamUniverse.Converter.VCE.Services;

/// <summary>
///     Formatting service
/// </summary>
public class FormattingService : IFormattingService
{
    private const string FormatLine = "<br>";

    private readonly List<FormatFontModel> _formatFonts =
    [
        new FormatFontModel
        {
            Type = FormatFontType.Default,
            Start = "",
            End = ""
        },

        new FormatFontModel
        {
            Type = FormatFontType.Bold,
            Start = "<b>",
            End = "</b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Monospaced,
            Start = "<span style=\"font-family: Courier, monospace\">",
            End = "</span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.MonospacedBold,
            Start = "<span style=\"font-family: Courier, monospace\"><b>",
            End = "</b></span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.LargeBold,
            Start = "<span style=\"font-size: large\"><b>",
            End = "</b></span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Underline,
            Start = "<u>",
            End = "</u>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Italic,
            Start = "<i>",
            End = "</i>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.UnderlineHyperlink,
            Start = "<u><a href=\"UrlData\">UrlData",
            End = "</a></u>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldUnderline,
            Start = "<b><u>",
            End = "</u></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Small,
            Start = "<span style=\"font-size: small\">",
            End = "</span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Large,
            Start = "<span style=\"font-size: large\">",
            End = "</span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Hyperlink,
            Start = "<a href=\"UrlData\">UrlData",
            End = "</a>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.ItalicHyperlink,
            Start = "<i><a href=\"UrlData\">UrlData",
            End = "</a></i>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldHyperlink,
            Start = "<b><a href=\"UrlData\">UrlData",
            End = "</a></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldItalicHyperlink,
            Start = "<b><i><a href=\"UrlData\">UrlData",
            End = "</a><i></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldItalicUnderlineHyperlink,
            Start = "<b><i><u><a href=\"UrlData\">UrlData",
            End = "</a></u><i></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldItalic,
            Start = "<b><i>",
            End = "</i></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.BoldItalicUnderline,
            Start = "<b><i><u>",
            End = "</u></i></b>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.MonospacedSmall,
            Start = "<span style=\"font-family: Courier, monospace; font-size: small\">",
            End = "</span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.MonospacedLarge,
            Start = "<span style=\"font-family: Courier, monospace; font-size: large\">",
            End = "</span>"
        },

        new FormatFontModel
        {
            Type = FormatFontType.Italic,
            Start = "<i><u>",
            End = "</u></i>"
        }
    ];

    private readonly List<FormatLineModel> _formatLines =
    [
        new FormatLineModel
        {
            Type = FormatLineType.Delete,
            Start = "",
            End = ""
        },

        new FormatLineModel
        {
            Type = FormatLineType.Left,
            Start = "",
            End = ""
        },

        new FormatLineModel() // TODO az-102
        {
            Type = FormatLineType.SubParagraph,
            Start = "",
            End = ""
        },

        new FormatLineModel
        {
            Type = FormatLineType.Center,
            Start = "<div style=\"text-align: center\">",
            End = "</div>"
        },

        new FormatLineModel
        {
            Type = FormatLineType.Right,
            Start = "<div style=\"text-align: right\">",
            End = "</div>"
        },

        new FormatLineModel
        {
            Type = FormatLineType.Justify,
            Start = "<div style=\"text-align: justify\">",
            End = "</div>"
        },

        new FormatLineModel() // TODO az-304 microsoft.az-304.v2021-05-18.by.sienna.102q.eudump
        {
            Type = FormatLineType.NextLine,
            Start = "",
            End = ""
        },

        new FormatLineModel() // TODO AZ-400 microsoft.az-400.v2021-03-26.by.georgia.165q.eudump
        {
            Type = FormatLineType.NewUi,
            Start = "",
            End = ""
        },

        new FormatLineModel() // TODO AZ-400 microsoft.az-400.v2021-03-26.by.georgia.165q.eudump
        {
            Type = FormatLineType.NewUi2,
            Start = "",
            End = ""
        }
    ];

    private readonly List<string> _formatPatterns =
    [
        """^(-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (".*")$""",
        @"^(-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+)$",
        @"^(-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+)$",
        @"^(-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+)$"
    ];

    // TODO az-102
    // TODO az-304 microsoft.az-304.v2021-05-18.by.sienna.102q.eudump
    // TODO AZ-400 microsoft.az-400.v2021-03-26.by.georgia.165q.eudump
    // TODO AZ-400 microsoft.az-400.v2021-03-26.by.georgia.165q.eudump

    /// <summary>
    ///     Formatting exam question
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="question"></param>
    /// <param name="variantsCount"></param>
    public void FormattingExamQuestion(ExamQuestionModel examQuestionModel, byte[] question, int variantsCount)
    {
        byte[] splitter = [0x2d, 0x38, 0x20, 0x31, 0x20, 0x33, 0x20, 0x31];
        var separator = new Separator(question, splitter);

        separator.Pop();

        if (separator.Peek().SequenceEqual(splitter)) separator.Pop();

        var questionSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            questionSection = questionSection.Take(questionSection.Length - 5).ToArray();
            separator.Pop();
        }

        var questionSectionText = FormatText(questionSection);
        examQuestionModel.Question = questionSectionText;

        for (var i = 0; i < variantsCount; i++)
        {
            var variantSection = separator.Pop();

            if (separator.Peek().SequenceEqual(splitter))
            {
                variantSection = variantSection.Take(variantSection.Length - 5).ToArray();
                separator.Pop();
            }

            var variantSectionText = FormatText(variantSection);
            examQuestionModel.Variants.Add(variantSectionText);
        }

        var referenceSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            referenceSection = referenceSection.Take(referenceSection.Length - 5).ToArray();
            separator.Pop();
        }

        var referenceSectionText = FormatText(referenceSection);
        examQuestionModel.Reference = referenceSectionText;
    }

    /// <summary>
    ///     Formatting exam question area
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="question"></param>
    public void FormattingExamQuestionArea(ExamQuestionModel examQuestionModel, byte[] question)
    {
        var splitter = "-8 1 3 1"u8.ToArray();
        var separator = new Separator(question, splitter);

        separator.Pop();

        if (separator.Peek().SequenceEqual(splitter)) separator.Pop();

        var questionSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            questionSection = questionSection.Take(questionSection.Length - 5).ToArray();
            separator.Pop();
        }

        var questionSectionText = FormatText(questionSection);
        examQuestionModel.Question = questionSectionText;

        var referenceSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            referenceSection = referenceSection.Take(referenceSection.Length - 5).ToArray();
            separator.Pop();
        }

        var referenceSectionText = FormatText(referenceSection);
        examQuestionModel.Reference = referenceSectionText;
    }

    /// <summary>
    ///     Formatting exam question block
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="question"></param>
    public void FormattingExamQuestionBlock(ExamQuestionModel examQuestionModel, byte[] question)
    {
        byte[] splitter = [0x2d, 0x38, 0x20, 0x31, 0x20, 0x33, 0x20, 0x31];
        var separator = new Separator(question, splitter);

        separator.Pop();

        if (separator.Peek().SequenceEqual(splitter)) separator.Pop();

        var questionSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            questionSection = questionSection.Take(questionSection.Length - 5).ToArray();
            separator.Pop();
        }

        var questionSectionText = FormatText(questionSection);
        examQuestionModel.Question = questionSectionText;

        var referenceSection = separator.Pop();

        if (separator.Peek().SequenceEqual(splitter))
        {
            referenceSection = referenceSection.Take(referenceSection.Length - 5).ToArray();
            separator.Pop();
        }

        var referenceSectionText = FormatText(referenceSection);
        examQuestionModel.Reference = referenceSectionText;
    }

    /// <summary>
    ///     Formatting exam question area image
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="image"></param>
    public void FormattingExamQuestionAreaImage(ExamQuestionModel examQuestionModel, byte[] image)
    {
        var imageBase64 = Convert.ToBase64String(image, 0, image.Length);
        examQuestionModel.AreaImage = $"<img src=\"data:image;base64,{imageBase64}\" >";
    }

    /// <summary>
    ///     Formatting exam question hot area variants
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="variants"></param>
    public void FormattingExamQuestionHotAreaVariants(ExamQuestionModel examQuestionModel, byte[] variants)
    {
        var binaryReader = new BinaryReader(new MemoryStream(variants));

        examQuestionModel.HotAreasCount = binaryReader.ReadInt32();

        for (var i = 0; i < examQuestionModel.HotAreasCount; i++)
        {
            var startX = binaryReader.ReadInt32();
            var startY = binaryReader.ReadInt32();

            var endX = binaryReader.ReadInt32();
            var endY = binaryReader.ReadInt32();

            examQuestionModel.HotAreas.Add(new HotAreaModel
            {
                X = startX,
                Y = startY,
                Width = endX - startX,
                Height = endY - startY
            });
        }
    }

    /// <summary>
    ///     Formatting exam question drag and drop area variants
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="variants"></param>
    /// <param name="image"></param>
    public void FormattingExamQuestionDragAndDropAreaVariants(ExamQuestionModel examQuestionModel, byte[] variants,
        byte[] image)
    {
        var binaryReader = new BinaryReader(new MemoryStream(variants));

        var count = binaryReader.ReadInt32();

        for (var i = 0; i < count; i++)
        {
            var type = binaryReader.ReadByte();
            examQuestionModel.DragAndDropTypes.Add(type);

            binaryReader.ReadInt32();

            var startX = binaryReader.ReadInt32();
            var startY = binaryReader.ReadInt32();

            var endX = binaryReader.ReadInt32();
            var endY = binaryReader.ReadInt32();

            switch (type)
            {
                case 1:
                {
                    byte[] imageCroppedBytes;

                    using (var imageStream = new MemoryStream(image))
                    {
                        // Load the image with ImageSharp
                        using (var imageBitmap = Image.Load<Rgba32>(imageStream))
                        {
                            // Crop the image
                            var cropRectangle = new Rectangle(startX, startY, endX - startX, endY - startY);
                            imageBitmap.Mutate(x => x.Crop(cropRectangle));

                            // Save the cropped image to a new memory stream
                            using (var memoryStream = new MemoryStream())
                            {
                                imageBitmap.Save(memoryStream, new PngEncoder());
                                imageCroppedBytes = memoryStream.ToArray();
                            }
                        }
                    }

                    var imageBase64 = Convert.ToBase64String(imageCroppedBytes, 0, imageCroppedBytes.Length);
                    var imageHtml = $"<img src=\"data:image;base64,{imageBase64}\" >";

                    examQuestionModel.DragAreasCount += 1;
                    examQuestionModel.DragAreas.Add(new DragAndDropAreaModel
                    {
                        X = startX,
                        Y = startY,
                        Width = endX - startX,
                        Height = endY - startY,
                        Text = imageHtml
                    });
                    break;
                }
                case 2:
                    examQuestionModel.DropAreasCount += 1;
                    examQuestionModel.DropAreas.Add(new DragAndDropAreaModel
                    {
                        X = startX,
                        Y = startY,
                        Width = endX - startX,
                        Height = endY - startY
                    });
                    break;
                default:
                    throw new Exception("Drag and drop area not found");
            }
        }
    }

    /// <summary>
    ///     Formatting question answers
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="answers"></param>
    public void FormattingExamQuestionAnswers(ExamQuestionModel examQuestionModel, IEnumerable<byte> answers)
    {
        examQuestionModel.Answers = answers.GetStringWithReplaced();
    }

    /// <summary>
    ///     Formatting exam question hot area answers
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="answers"></param>
    public void FormattingExamQuestionHotAreaAnswers(ExamQuestionModel examQuestionModel, byte[] answers)
    {
        var binaryReader = new BinaryReader(new MemoryStream(answers));

        var count = binaryReader.ReadInt32();

        var numbers = new List<string>();

        for (var i = 0; i < count; i++)
        {
            var check = binaryReader.ReadByte();

            if (check != 1) continue;

            var numberText = (i + 1).ToString();
            numbers.Add(numberText);
        }

        examQuestionModel.Answers = string.Join(",", numbers);
    }

    /// <summary>
    ///     Formatting exam question drag and drop area answers
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="answers"></param>
    public void FormattingExamQuestionDragAndDropAreaAnswers(ExamQuestionModel examQuestionModel, byte[] answers)
    {
        var binaryReader = new BinaryReader(new MemoryStream(answers));

        var dragNumbers = new List<string>();
        var dropNumbers = new List<string>();

        var count = binaryReader.ReadInt32();

        var index = 0;

        for (var i = 0; i < count; i++)
        {
            var number = binaryReader.ReadInt32();

            if (number == -1)
            {
                index += 1;
                dragNumbers.Add((index - 1).ToString());
                continue;
            }

            switch (examQuestionModel.DragAndDropTypes[index])
            {
                case 1:
                    index += 1;
                    dragNumbers.Add(number.ToString());
                    break;
                case 2:
                    index += 1;
                    dropNumbers.Add(number.ToString());
                    break;
                default:
                    throw new Exception("Drag and drop area not found");
            }
        }

        if (examQuestionModel.DragAndDropTypes.Count - index > 0)
        {
            var exceptNumbers = dropNumbers.Except(dragNumbers).ToList();
            dragNumbers.AddRange(exceptNumbers);

            dragNumbers = dragNumbers.OrderBy(Convert.ToInt32).ToList();
        }

        var numbers = new List<string>();

        foreach (var dropNumber in dropNumbers)
            for (var j = 0; j < dragNumbers.Count; j++)
            {
                var dragNumber = dragNumbers[j];

                if (dropNumber != dragNumber) continue;

                var numberText = (j + 1).ToString();
                numbers.Add(numberText);
            }

        examQuestionModel.Answers = string.Join(",", numbers);
    }

    /// <summary>
    ///     Formatting question block answers
    /// </summary>
    /// <param name="examQuestionModel"></param>
    /// <param name="answers"></param>
    /// <param name="answersCount"></param>
    public void FormattingExamQuestionBlockAnswers(ExamQuestionModel examQuestionModel, byte[] answers,
        int answersCount)
    {
        byte[] splitter = [0x02, 0x07];
        var separator = new Separator(answers, splitter);

        var answersList = new List<string?>();

        separator.Pop();

        for (var i = 0; i < answersCount; i++)
        {
            separator.Pop();

            var answerSection = separator.Pop();

            answerSection = i + 1 < answersCount
                ? answerSection.Take(answerSection.Length - 3).ToArray()
                : answerSection.ToArray();

            answersList.Add(answerSection.GetString());
        }

        // TODO rewrite this in order to save a single sheet of answers.
        examQuestionModel.BlockAnswersCount = answersList.Count;
        examQuestionModel.BlockAnswers = answersList;
    }

    /// <summary>
    ///     Formatting description
    /// </summary>
    /// <param name="examSectionModel"></param>
    /// <param name="description"></param>
    public void FormattingDescription(ExamSectionModel examSectionModel, byte[] description)
    {
        byte[] splitter = [0x2d, 0x38, 0x20, 0x31, 0x20, 0x33, 0x20, 0x31];
        var separator = new Separator(description, splitter);

        if (separator.Peek().SequenceEqual(splitter)) separator.Pop();

        var descriptionSection = separator.Pop();
        var descriptionSectionText = FormatText(descriptionSection);
        examSectionModel.Description = descriptionSectionText;
    }

    /// <summary>
    ///     Format text
    /// </summary>
    /// <param name="bytes"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns></returns>
    private string FormatText(byte[] bytes)
    {
        byte[] splitterBlock = [0x0d, 0x0a];
        byte[] splitterLine = [0x29, 0x20];

        var separator = new Separator(bytes, [splitterBlock, splitterLine]);
        var dataArray = GetDataBySplitters(separator, splitterBlock, splitterLine);

        var resultString = new List<string?>();

        var formatLineEnd = "";

        var formatTextEnd = "";

        foreach (var data in dataArray)
            switch (data.Type)
            {
                case DataType.Text:
                {
                    var textDataString = data.Data.GetString();
                    resultString.Add(textDataString);
                    resultString.Add(FormatLine);
                    break;
                }
                case DataType.Format:
                {
                    var formatDataString = data.Data.GetString();
                    var formatData = SplitStringByPatterns(formatDataString);

                    switch (formatData.Count)
                    {
                        case 0:
                            break;
                        case 6:
                        {
                            var formatFontNumber = Convert.ToInt32(formatData[0]);
                            var formatFontType = (FormatFontType)formatFontNumber;

                            var formatLineNumber = Convert.ToInt32(formatData[2]);
                            var formatLineType = (FormatLineType)formatLineNumber;

                            var url = formatData[5];

                            // Format line
                            var formatLine = _formatLines.FirstOrDefault(fl => fl.Type == formatLineType);

                            if (formatLine is { Type: FormatLineType.Delete })
                            {
                                if (resultString.LastOrDefault() == FormatLine)
                                    resultString.RemoveAt(resultString.Count - 1);
                            }
                            else
                            {
                                resultString.Add(formatLineEnd);

                                if (formatLine != null)
                                {
                                    var formatLineStart = formatLine.Start;
                                    formatLineEnd = formatLine.End;

                                    resultString.Add(formatLineStart);
                                }
                            }

                            // Format text
                            var formatText = _formatFonts.FirstOrDefault(ft => ft.Type == formatFontType);

                            // TODO Set default font because idn how create 17 font
                            if (formatFontNumber >= 17) formatText = _formatFonts.FirstOrDefault();

                            resultString.Add(formatTextEnd);

                            if (formatText?.Start != null)
                            {
                                formatTextEnd = formatText.End;
                                resultString.Add(formatText.Start.Replace("UrlData", url));
                            }

                            break;
                        }
                        case 7:
                            break;
                        case 10:
                            resultString.Add("• ");
                            resultString.Add(FormatLine);
                            break;
                        default:
                            throw new Exception("Format type not found");
                    }

                    break;
                }
                case DataType.Image:
                {
                    var imageWidth = $"{data.Property1.GetStringWithReplaced().Replace("=", "=\"")}\"";
                    var imageHeight = $"{data.Property1.GetStringWithReplaced().Replace("=", "=\"")}\"";

                    if (data.Data != null)
                    {
                        var imageBase64 = Convert.ToBase64String(data.Data, 0, data.Data.Length);
                        resultString.Add($"<img src=\"data:image;base64,{imageBase64}\" {imageWidth} {imageHeight} >");
                    }

                    break;
                }
                default:
                    throw new Exception("Data type not found");
            }

        return string.Join("", resultString).TrimStart(FormatLine).TrimEnd(FormatLine);
    }

    /// <summary>
    ///     Get data by splitters
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="splitterBlock"></param>
    /// <param name="splitterLine"></param>
    /// <returns></returns>
    private static List<DataModel> GetDataBySplitters(Separator separator, byte[] splitterBlock, byte[] splitterLine)
    {
        var dataModels = new List<DataModel>();
        var data = Array.Empty<byte>();

        while (separator.HasValue)
        {
            var temp = separator.Pop();
            var tempString = temp.GetString();

            if (temp.SequenceEqual(splitterLine))
            {
                dataModels.Add(new DataModel
                {
                    Data = data,
                    Type = DataType.Text
                });

                data = Array.Empty<byte>();
                continue;
            }

            if (temp.SequenceEqual(splitterBlock))
            {
                dataModels.Add(new DataModel
                {
                    Data = data,
                    Type = DataType.Format
                });

                data = Array.Empty<byte>();
                continue;
            }

            if (tempString.Contains("TJPEGImage") || tempString.Contains("TPngImage"))
            {
                var imageWidthProperty = Array.Empty<byte>();
                var imageHeightProperty = Array.Empty<byte>();

                SkipSplitters(separator, splitterBlock, splitterLine);

                if (separator.Peek().GetString().Contains("width"))
                {
                    imageWidthProperty = separator.Pop();
                    SkipSplitters(separator, splitterBlock, splitterLine);
                }

                if (separator.Peek().GetString().Contains("height"))
                {
                    imageHeightProperty = separator.Pop();
                    SkipSplitters(separator, splitterBlock, splitterLine);
                }

                var imageLength = separator.ReadInt32();
                var imageData = separator.ReadBytes(imageLength);

                dataModels.Add(new DataModel
                {
                    Data = imageData,
                    Type = DataType.Image,

                    Property1 = imageWidthProperty,
                    Property2 = imageHeightProperty
                });

                data = Array.Empty<byte>();
                continue;
            }

            data = temp;
        }

        return dataModels;
    }

    /// <summary>
    ///     Skip splitters
    /// </summary>
    /// <param name="separator"></param>
    /// <param name="splitterBlock"></param>
    /// <param name="splitterLine"></param>
    private static void SkipSplitters(Separator separator, byte[] splitterBlock, byte[] splitterLine)
    {
        while (separator.HasValue)
        {
            var temp = separator.Peek();

            if (!temp.SequenceEqual(splitterBlock) && !temp.SequenceEqual(splitterLine)) break;

            separator.Pop();
        }
    }

    /// <summary>
    ///     Split string by patterns
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private List<string> SplitStringByPatterns(string? content)
    {
        var results = new List<string>();

        if (content is null) return results;

        foreach (var matches in _formatPatterns.Select(t => Regex.Match(content, t)).Select(matches => matches))
        {
            var match = matches;
            while (match.Success)
            {
                for (var j = 1; j < match.Groups.Count; j++) results.Add(match.Groups[j].Value);

                match = match.NextMatch();
            }
        }

        return results;
    }
}