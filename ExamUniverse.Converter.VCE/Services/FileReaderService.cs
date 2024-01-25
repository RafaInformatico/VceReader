using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ExamUniverse.Converter.VCE.Enums;
using ExamUniverse.Converter.VCE.Extensions;
using ExamUniverse.Converter.VCE.Models.FileReader;
using ExamUniverse.Converter.VCE.Services.Interfaces;

namespace ExamUniverse.Converter.VCE.Services;

/// <summary>
///     File reader service
/// </summary>
public class FileReaderService : IFileReaderService
{
    private readonly IFormattingService _formattingService;

    public FileReaderService(IFormattingService formattingService)
    {
        _formattingService = formattingService;
    }

    /// <summary>
    ///     Read file
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="encryptKeys"></param>
    /// <param name="decryptKeys"></param>
    /// <returns></returns>
    public FileModel ReadFile(byte[] bytes, byte[] encryptKeys, byte[] decryptKeys)
    {
        var fileModel = new FileModel
        {
            EncryptKeys = encryptKeys,
            DecryptKeys = decryptKeys
        };

        using var binaryReader = new BinaryReader(new MemoryStream(bytes));
        var headerFirstByte = binaryReader.ReadByte();
        var headerSecondByte = binaryReader.ReadByte();

        // Check header bytes
        if (headerFirstByte != 0x85 || headerSecondByte != 0xA8)
        {
            throw new Exception("Invalid file format");
        }

        #region Read version

        var versionFirstByte = binaryReader.ReadByte();
        var versionSecondByte = binaryReader.ReadByte();
        fileModel.Version = versionFirstByte * 10 + versionSecondByte;

        #endregion

        #region Read keys

        binaryReader.ReadInt32();

        // Get keys for exam
        fileModel.Keys = ReadArray(binaryReader);
        ReadArray(binaryReader);

        #endregion

        #region Read information file

        binaryReader.ReadByte();

        fileModel.Number = ReadDecryptArray(binaryReader, fileModel).GetString();
        fileModel.Title = ReadDecryptArray(binaryReader, fileModel).GetString();

        fileModel.PassingScore = binaryReader.ReadInt32();
        fileModel.TimeLimit = binaryReader.ReadInt32();

        fileModel.FileVersion = ReadDecryptArray(binaryReader, fileModel).GetString();

        binaryReader.ReadInt64();
        binaryReader.ReadInt64();

        // delete from 62 version file
        if (fileModel.Version <= 61)
        {
            binaryReader.ReadInt64();
            binaryReader.ReadInt64();

            binaryReader.ReadByte();
            binaryReader.ReadByte();

            ReadDecryptArray(binaryReader, fileModel).GetString();
        }

        #endregion

        #region Read styles

        ReadDecryptArray(binaryReader, fileModel).GetString();

        #endregion

        #region Read description

        ReadDecryptArray(binaryReader, fileModel).GetString();

        #endregion

        #region Read sections

        fileModel.SectionsCount = binaryReader.ReadInt32();

        for (var i = 1; i <= fileModel.SectionsCount; i++)
        {
            var sectionModel = new SectionModel
            {
                Id = binaryReader.ReadInt32(),
                Name = ReadDecryptArray(binaryReader, fileModel).GetString()
            };

            fileModel.Sections.Add(sectionModel);
        }

        #endregion

        // Read TODO
        ReadDecryptArray(binaryReader, fileModel).GetString();

        // Exams count
        var examsCount = binaryReader.ReadInt32();

        // Read all exams
        for (var i = 0; i < examsCount; i++)
        {
            var examModel = new ExamModel
            {
                Id = fileModel.Exams.Count + 1,
                Type = (ExamType)binaryReader.ReadByte(),
                Name = ReadDecryptArray(binaryReader, fileModel).GetString()
            };

            switch (examModel.Type)
            {
                case ExamType.Question:
                {
                    var examQuestionsCount = binaryReader.ReadInt32();
                    ReadExamQuestions(binaryReader, fileModel, examModel, examQuestionsCount);
                    break;
                }
                case ExamType.Section:
                {
                    var examSectionsCount = binaryReader.ReadInt32();
                    ReadExamSections(binaryReader, fileModel, examModel, examSectionsCount);
                    break;
                }
                default:
                    throw new Exception("Exam type not found");
            }

            fileModel.ExamsCount += 1;
            fileModel.Exams.Add(examModel);
        }

        // File length
        var fileLength = binaryReader.ReadInt32();

        if (binaryReader.BaseStream.Length != fileLength)
        {
            throw new Exception("Invalid file format");
        }

        return fileModel;
    }

    /// <summary>
    ///     Read exam questions
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <param name="fileModel"></param>
    /// <param name="examModel"></param>
    /// <param name="examQuestionCount"></param>
    /// <param name="examSectionId"></param>
    private void ReadExamQuestions(BinaryReader binaryReader, FileModel fileModel, ExamModel examModel,
        int examQuestionCount, int examSectionId = -1)
    {
        for (var i = 0; i < examQuestionCount; i++)
        {
            var examQuestionModel = new ExamQuestionModel
            {
                Id = examModel.ExamQuestionsCount + 1,
                ExamSectionId = examSectionId
            };

            if (fileModel.Version >= 61)
            {
                GetArrayLength(binaryReader);
            }

            ReadDecryptArray(binaryReader, fileModel);

            examQuestionModel.Type = (ExamQuestionType)binaryReader.ReadByte();

            examQuestionModel.SectionId = binaryReader.ReadInt32();
            examQuestionModel.Complexity = binaryReader.ReadInt32();

            binaryReader.ReadInt32();

            switch (examQuestionModel.Type)
            {
                case ExamQuestionType.SingleChoice:
                case ExamQuestionType.MultipleChoice:
                {
                    var questionBytes = ReadDecryptArray(binaryReader, fileModel);
                    var answersBytes = ReadDecryptArray(binaryReader, fileModel);

                    examQuestionModel.VariantsCount = binaryReader.ReadInt32();

                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();

                    ReadDecryptArray(binaryReader, fileModel);

                    _formattingService.FormattingExamQuestion(examQuestionModel, questionBytes,
                        examQuestionModel.VariantsCount);
                    _formattingService.FormattingExamQuestionAnswers(examQuestionModel, answersBytes);
                    break;
                }
                case ExamQuestionType.HotArea:
                {
                    var variantsBytes = ReadDecryptArray(binaryReader, fileModel);
                    var answersBytes = ReadDecryptArray(binaryReader, fileModel);
                    var questionBytes = ReadDecryptArray(binaryReader, fileModel);

                    binaryReader.ReadByte();
                    binaryReader.ReadByte();

                    var image = ReadDecryptArray(binaryReader, fileModel);

                    _formattingService.FormattingExamQuestionArea(examQuestionModel, questionBytes);
                    _formattingService.FormattingExamQuestionAreaImage(examQuestionModel, image);
                    _formattingService.FormattingExamQuestionHotAreaVariants(examQuestionModel, variantsBytes);
                    _formattingService.FormattingExamQuestionHotAreaAnswers(examQuestionModel, answersBytes);
                    break;
                }
                case ExamQuestionType.DragAndDrop:
                {
                    var variantsBytes = ReadDecryptArray(binaryReader, fileModel);
                    var answersBytes = ReadDecryptArray(binaryReader, fileModel);
                    var questionBytes = ReadDecryptArray(binaryReader, fileModel);

                    binaryReader.ReadByte();
                    binaryReader.ReadByte();

                    var image = ReadDecryptArray(binaryReader, fileModel);

                    _formattingService.FormattingExamQuestionArea(examQuestionModel, questionBytes);
                    _formattingService.FormattingExamQuestionAreaImage(examQuestionModel, image);
                    _formattingService.FormattingExamQuestionDragAndDropAreaVariants(examQuestionModel, variantsBytes,
                        image);
                    _formattingService.FormattingExamQuestionDragAndDropAreaAnswers(examQuestionModel, answersBytes);
                    break;
                }
                case ExamQuestionType.FillInTheBlank:
                {
                    var questionBytes = ReadDecryptArray(binaryReader, fileModel);
                    var answersBytes = ReadDecryptArray(binaryReader, fileModel);

                    var answersCount = binaryReader.ReadInt32();

                    binaryReader.ReadByte();
                    binaryReader.ReadByte();
                    binaryReader.ReadByte();

                    _formattingService.FormattingExamQuestionBlock(examQuestionModel, questionBytes);
                    _formattingService.FormattingExamQuestionBlockAnswers(examQuestionModel, answersBytes, answersCount);
                    break;
                }
                default:
                    throw new Exception("Exam question type not found");
            }

            examModel.ExamQuestionsCount += 1;
            examModel.ExamQuestions.Add(examQuestionModel);
        }
    }

    /// <summary>
    ///     Read exam sections
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <param name="fileModel"></param>
    /// <param name="examModel"></param>
    /// <param name="examSectionCount"></param>
    private void ReadExamSections(BinaryReader binaryReader, FileModel fileModel, ExamModel examModel,
        int examSectionCount)
    {
        for (var i = 0; i < examSectionCount; i++)
        {
            var examSectionModel = new ExamSectionModel
            {
                Id = examModel.ExamSectionsCount + 1,
                Type = (ExamSectionType)binaryReader.ReadByte()
            };

            if (!Enum.IsDefined(typeof(ExamSectionType), examSectionModel.Type))
            {
                throw new Exception("Exam section type not found");
            }

            examSectionModel.TimeLimit = binaryReader.ReadInt32();

            switch (examSectionModel.Type)
            {
                case ExamSectionType.QuestionSet:
                {
                    var examQuestionsCount = binaryReader.ReadInt32();
                    ReadExamQuestions(binaryReader, fileModel, examModel, examQuestionsCount, examSectionModel.Id);
                    break;
                }
                case ExamSectionType.Testlet:
                {
                    examSectionModel.Title = ReadDecryptArray(binaryReader, fileModel).GetString();

                    binaryReader.ReadInt32();

                    ReadDecryptArray(binaryReader, fileModel).GetString();

                    var descriptionBytes = ReadDecryptArray(binaryReader, fileModel);
                    _formattingService.FormattingDescription(examSectionModel, descriptionBytes);

                    binaryReader.ReadInt32();

                    var examQuestionsCount = binaryReader.ReadInt32();
                    ReadExamQuestions(binaryReader, fileModel, examModel, examQuestionsCount, examSectionModel.Id);
                    break;
                }
                default:
                    throw new Exception("Section type not found");
            }

            examModel.ExamSectionsCount += 1;
            examModel.ExamSections.Add(examSectionModel);
        }
    }

    /// <summary>
    ///     Read array
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <returns></returns>
    private static byte[] ReadArray(BinaryReader binaryReader)
    {
        var messageLen = GetArrayLength(binaryReader);
        return binaryReader.ReadBytes(messageLen);
    }

    /// <summary>
    ///     Read decrypt array
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <param name="fileModel"></param>
    /// <returns></returns>
    private static byte[] ReadDecryptArray(BinaryReader binaryReader, FileModel fileModel)
    {
        var messageLen = GetArrayLength(binaryReader);

        if (messageLen == 0)
        {
            return Array.Empty<byte>();
        }

        messageLen -= 1;

        byte[] selectedKey;
        var slectKey = binaryReader.ReadByte();

        if (slectKey < 0x80)
        {
            if (fileModel.Keys == null || fileModel.Keys.Length == 0)
            {
                throw new Exception("Key cannot be empty");
            }

            selectedKey = fileModel.Keys;
        }
        else
        {
            if (fileModel.DecryptKeys == null || fileModel.DecryptKeys.Length == 0)
            {
                throw new Exception("Key cannot be empty");
            }

            selectedKey = fileModel.DecryptKeys;
        }

        var globalOffset = 0;

        if (fileModel.Version >= 61)
        {
            messageLen -= 4;
            globalOffset = binaryReader.ReadInt32();
        }

        var key = selectedKey.Skip(globalOffset).Take(32).ToArray();
        var iv = selectedKey.Skip(globalOffset).Skip(32).Take(16).ToArray();

        var encryptedBytes = binaryReader.ReadBytes(messageLen);
        var decryptedBytes = DecryptBytes_Aes(encryptedBytes, key, iv);
        return decryptedBytes;
    }

    /// <summary>
    ///     Get array length
    /// </summary>
    /// <param name="binaryReader"></param>
    /// <returns></returns>
    private static int GetArrayLength(BinaryReader binaryReader)
    {
        var bytes = new List<byte>();
        var vceChar = binaryReader.ReadByte();

        var v1 = GetArrayLength_Xor1(vceChar, vceChar ^ 0x80);
        var v2 = GetArrayLength_Xor2(0x80, v1, 0);

        var counter = 0;

        do
        {
            vceChar = binaryReader.ReadByte();
            var v4 = (v2 ^ vceChar) & 0xff;
            var v5 = GetArrayLength_Xor1(vceChar, v2 ^ vceChar);
            v2 = GetArrayLength_Xor2(v2, v5, counter + 1);

            bytes.Add((byte)v4);
            counter++;
        } while (counter != 4);

        return BitConverter.ToInt32(bytes.ToArray(), 0);
    }

    /// <summary>
    ///     Get array length Xor1
    /// </summary>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <returns></returns>
    private static int GetArrayLength_Xor1(int a1, int a2)
    {
        return (a1 ^ a2) & 0xff;
    }

    /// <summary>
    ///     Get array length Xor2
    /// </summary>
    /// <param name="a1"></param>
    /// <param name="a2"></param>
    /// <param name="a3"></param>
    /// <returns></returns>
    private static int GetArrayLength_Xor2(int a1, int a2, int a3)
    {
        return (a1 + a2) | a3;
    }

    /// <summary>
    ///     Decrypt bytes aes
    /// </summary>
    /// <param name="encryptedBytes"></param>
    /// <param name="key"></param>
    /// <param name="iv"></param>
    /// <returns></returns>
    private static byte[] DecryptBytes_Aes(byte[] encryptedBytes, byte[] key, byte[] iv)
    {
        using var aesCrypto = Aes.Create();
        aesCrypto.Padding = PaddingMode.None;

        aesCrypto.Key = key;
        aesCrypto.IV = iv;

        using var decryptor = aesCrypto.CreateDecryptor(aesCrypto.Key, aesCrypto.IV);
        using var msEncrypt = new MemoryStream(encryptedBytes);
        using var cryptoStream = new CryptoStream(msEncrypt, decryptor, CryptoStreamMode.Read);
        using var msDecrypt = new MemoryStream();
        cryptoStream.CopyTo(msDecrypt);
        return msDecrypt.ToArray();
    }
}