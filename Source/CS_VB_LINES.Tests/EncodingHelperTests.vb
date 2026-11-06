#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.ComponentModel
Imports System.IO
Imports System.Text

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

#Region " EncodingHelperTests "

''' <summary>
''' Tests for <see cref="EncodingHelper"/>: detection of the UTF encoding of a file 
''' (with BOM, headerless UTF-16/UTF-32, UTF-7 and strict UTF-8 validation) and mapping to <see cref="Encoding"/> instances.
''' <para></para>
''' The enumeration <see cref="UtfEncodingKind"/> is not public, so the data-driven tests receive its member names as text.
''' </summary>
Public Class EncodingHelperTests

#Region " Constants and Fields "

    ''' <summary>
    ''' Number of bytes that the detection reads from the start of a file. 
    ''' It mirrors the private constant of <see cref="EncodingHelper.TryGetUtfFileEncodingKind"/>.
    ''' </summary>
    Private Const SampleSize As Integer = 32768

    ''' <summary>
    ''' A text long enough for the heuristics of the headerless UTF-16 and UTF-32 detection.
    ''' </summary>
    Private Shared ReadOnly SampleText As String = $"Public Class Sample{vbLf}    ' Hello World, this is a long enough text.{vbLf}End Class{vbLf}"

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Converts the name of a member of <see cref="UtfEncodingKind"/> to its value.
    ''' </summary>
    Private Shared Function ParseKind(kindName As String) As UtfEncodingKind

        Return DirectCast([Enum].Parse(GetType(UtfEncodingKind), kindName), UtfEncodingKind)

    End Function

    ''' <summary>
    ''' Writes the bytes to a new file inside a temporary directory, and returns the detected kind (as text) 
    ''' and the value returned by the detection method.
    ''' </summary>
    Private Shared Function Detect(bytes As Byte(), ByRef refDetectedKindName As String) As Boolean

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("sample.txt", bytes)
            Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown
            Dim result As Boolean = EncodingHelper.TryGetUtfFileEncodingKind(filePath, detectedKind)

            refDetectedKindName = detectedKind.ToString()

            Return result

        End Using

    End Function

#End Region

#Region " GetEncodingFromUtfEncodingKind "

    <Fact>
    Public Sub GetEncodingFromUtfEncodingKind_ReturnsCorrectEncoding()

        Dim encUtf8 As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.UTF8)
        Dim encUtf8Bom As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.UTF8_Bom)
        Dim encUnknown As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.Unknown)

        Assert.NotNull(encUtf8)
        Assert.Equal(0, encUtf8.GetPreamble().Length)

        Assert.NotNull(encUtf8Bom)
        Assert.True(encUtf8Bom.GetPreamble().Length > 0, "BOM encoding should have a preamble.")

        Assert.Null(encUnknown)

    End Sub

    ''' <summary>
    ''' Every kind maps to the right code page and emits (or not) the right preamble, 
    ''' because the preamble is what makes the rewritten file keep its BOM.
    ''' </summary>
    <Theory>
    <InlineData("UTF7", 65000, "")>
    <InlineData("UTF8", 65001, "")>
    <InlineData("UTF8_Bom", 65001, "EFBBBF")>
    <InlineData("UTF16LE", 1200, "")>
    <InlineData("UTF16LE_Bom", 1200, "FFFE")>
    <InlineData("UTF16BE", 1201, "")>
    <InlineData("UTF16BE_Bom", 1201, "FEFF")>
    <InlineData("UTF32LE", 12000, "")>
    <InlineData("UTF32LE_Bom", 12000, "FFFE0000")>
    <InlineData("UTF32BE", 12001, "")>
    <InlineData("UTF32BE_Bom", 12001, "0000FEFF")>
    Public Sub GetEncodingFromUtfEncodingKind_MapsEachKindToItsCodePageAndPreamble(kindName As String,
                                                                                   expectedCodePage As Integer,
                                                                                   expectedPreambleHex As String)

        Dim result As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(EncodingHelperTests.ParseKind(kindName))

        Assert.NotNull(result)
        Assert.Equal(expectedCodePage, result.CodePage)
        Assert.Equal(expectedPreambleHex, Convert.ToHexString(result.GetPreamble()))

    End Sub

    <Theory>
    <InlineData(-1)>
    <InlineData(12)>
    <InlineData(999)>
    Public Sub GetEncodingFromUtfEncodingKind_UndefinedKind_ThrowsInvalidEnumArgumentException(value As Integer)

        Assert.Throws(Of InvalidEnumArgumentException)(
            Function() EncodingHelper.GetEncodingFromUtfEncodingKind(CType(value, UtfEncodingKind)))

    End Sub

    ''' <summary>
    ''' The BOM-less UTF-8 encoding must not emit a preamble when it is used to write a file.
    ''' </summary>
    <Fact>
    Public Sub GetEncodingFromUtfEncodingKind_UTF8_WritesWithoutBom()

        Dim encoding As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.UTF8)

        Using stream As New MemoryStream()
            Using writer As New StreamWriter(stream, encoding)
                writer.Write("abc")
            End Using

            Assert.Equal("616263", Convert.ToHexString(stream.ToArray()))
        End Using

    End Sub

#End Region

#Region " TryGetUtfFileEncodingKind: files with BOM "

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_WithUTF8BOM_DetectsSuccessfully()

        Dim kindName As String = Nothing
        Dim bytes As Byte() = TestEncodings.Encode("Hello World", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True))

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("UTF8_Bom", kindName)

    End Sub

    ''' <summary>
    ''' All the BOM variants are recognized, in particular the UTF-32LE one (FF FE 00 00), 
    ''' which starts exactly like the UTF-16LE one (FF FE) and must be checked first.
    ''' </summary>
    <Theory>
    <InlineData("UTF8_Bom")>
    <InlineData("UTF16LE_Bom")>
    <InlineData("UTF16BE_Bom")>
    <InlineData("UTF32LE_Bom")>
    <InlineData("UTF32BE_Bom")>
    Public Sub TryGetUtfFileEncodingKind_FileWithBom_IsDetected(encodingName As String)

        Dim bytes As Byte() = TestEncodings.Encode(SampleText, TestEncodings.Create(encodingName))
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal(encodingName, kindName)

    End Sub

    ''' <summary>
    ''' A file made only of a BOM (no text) is still detected.
    ''' </summary>
    <Theory>
    <InlineData("EFBBBF", "UTF8_Bom")>
    <InlineData("FFFE", "UTF16LE_Bom")>
    <InlineData("FEFF", "UTF16BE_Bom")>
    <InlineData("FFFE0000", "UTF32LE_Bom")>
    <InlineData("0000FEFF", "UTF32BE_Bom")>
    Public Sub TryGetUtfFileEncodingKind_FileWithOnlyTheBom_IsDetected(bomHex As String, expectedKindName As String)

        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(Convert.FromHexString(bomHex), kindName))
        Assert.Equal(expectedKindName, kindName)

    End Sub

    ''' <summary>
    ''' Writing a text with an encoding and then detecting it returns the same kind for every supported variant. 
    ''' Text with accents, CJK and a surrogate pair is used, since that is what real source files contain.
    ''' </summary>
    <Theory>
    <InlineData("UTF8")>
    <InlineData("UTF8_Bom")>
    <InlineData("UTF16LE")>
    <InlineData("UTF16LE_Bom")>
    <InlineData("UTF16BE")>
    <InlineData("UTF16BE_Bom")>
    <InlineData("UTF32LE")>
    <InlineData("UTF32LE_Bom")>
    <InlineData("UTF32BE")>
    <InlineData("UTF32BE_Bom")>
    Public Sub TryGetUtfFileEncodingKind_RoundTripOfEveryVariant_ReturnsTheSameKind(encodingName As String)

        Dim text As String = $"' Ñandú — Café — 日本語 {ChrW(&HD83D)}{ChrW(&HDE00)}{vbLf}Public Class Sample{vbLf}End Class{vbLf}"
        Dim bytes As Byte() = TestEncodings.Encode(text, TestEncodings.Create(encodingName))
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal(encodingName, kindName)

    End Sub

    ''' <summary>
    ''' After detecting the kind, the encoding returned by the helper decodes the file back to the original text 
    ''' (this is the exact sequence that the application performs before rewriting a file).
    ''' </summary>
    <Theory>
    <InlineData("UTF8")>
    <InlineData("UTF8_Bom")>
    <InlineData("UTF16LE_Bom")>
    <InlineData("UTF16BE")>
    <InlineData("UTF32LE")>
    <InlineData("UTF32BE_Bom")>
    Public Sub TryGetUtfFileEncoding_ThenReadAllText_RecoversTheOriginalText(encodingName As String)

        Dim text As String = $"' Ñandú — Café — 日本語{vbLf}Public Class Sample{vbLf}End Class{vbLf}"

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteText("sample.vb", text, TestEncodings.Create(encodingName))
            Dim detectedEncoding As Encoding = Nothing

            Assert.True(EncodingHelper.TryGetUtfFileEncoding(filePath, detectedEncoding))
            Assert.NotNull(detectedEncoding)
            Assert.Equal(text, File.ReadAllText(filePath, detectedEncoding))

        End Using

    End Sub

#End Region

#Region " TryGetUtfFileEncodingKind: headerless files "

    ''' <summary>
    ''' UTF-16 and UTF-32 without BOM, written from ASCII source code (the typical case), are detected through the 
    ''' distribution of their NUL bytes, with the right endianness.
    ''' </summary>
    <Theory>
    <InlineData("UTF16LE")>
    <InlineData("UTF16BE")>
    <InlineData("UTF32LE")>
    <InlineData("UTF32BE")>
    Public Sub TryGetUtfFileEncodingKind_HeaderlessAsciiText_IsDetectedByNulPattern(encodingName As String)

        Dim bytes As Byte() = TestEncodings.Encode(SampleText, TestEncodings.Create(encodingName))
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal(encodingName, kindName)

    End Sub

    ''' <summary>
    ''' A binary file (many NUL bytes in no recognizable pattern) is rejected instead of guessed.
    ''' </summary>
    <Fact>
    Public Sub TryGetUtfFileEncodingKind_BinaryData_IsRejected()

        Dim bytes(255) As Byte
        For index As Integer = 0 To bytes.Length - 1
            bytes(index) = CByte(index) ' 0x00..0xFF: contains bytes that are invalid in UTF-8.
        Next index
        Dim kindName As String = Nothing

        Assert.False(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("Unknown", kindName)

    End Sub

#End Region

#Region " TryGetUtfFileEncodingKind: UTF-8 "

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_PlainAscii_IsUtf8()

        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(Encoding.ASCII.GetBytes(SampleText), kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_SingleAsciiByte_IsUtf8()

        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(New Byte() {&H61}, kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_Utf8WithMultibyteCharactersAndNoBom_IsUtf8()

        Dim bytes As Byte() = New UTF8Encoding(False).GetBytes($"' Ñandú — 日本語 {ChrW(&HD83D)}{ChrW(&HDE00)}{vbLf}")
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

    ''' <summary>
    ''' Legacy single-byte encodings (Latin-1 / Windows-1252) are not UTF-8: they must be rejected 
    ''' so the application skips the file instead of corrupting it.
    ''' </summary>
    <Theory>
    <InlineData("63616665E92063616665")>
    <InlineData("80")>
    <InlineData("C328")>
    <InlineData("E282")>
    <InlineData("C0AF")>
    <InlineData("FF616263")>
    <InlineData("F0288C28")>
    Public Sub TryGetUtfFileEncodingKind_InvalidUtf8Sequences_AreRejected(hexBytes As String)

        ' 63616665E9... : "cafe" + 0xE9 (Latin-1 "é") in the middle of the text.
        ' 80            : lone continuation byte.
        ' C328          : lead byte followed by a non-continuation byte.
        ' E282          : 3-byte sequence cut after two bytes, followed by text ("E282" + "20..." below).
        ' C0AF          : overlong encoding of '/'.
        ' FF616263      : 0xFF never appears in UTF-8.
        ' F0288C28      : 4-byte lead followed by invalid continuations.
        Dim padding As Byte() = Encoding.ASCII.GetBytes(" trailing text that follows the invalid bytes")
        Dim invalid As Byte() = Convert.FromHexString(hexBytes)
        Dim bytes(invalid.Length + padding.Length - 1) As Byte
        Buffer.BlockCopy(invalid, 0, bytes, 0, invalid.Length)
        Buffer.BlockCopy(padding, 0, bytes, invalid.Length, padding.Length)
        Dim kindName As String = Nothing

        Assert.False(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("Unknown", kindName)

    End Sub

    ''' <summary>
    ''' A UTF-8 multibyte character that is cut by the end of the 32 KB sample must not make a valid file look invalid. 
    ''' Every cut position of 2, 3 and 4-byte characters is covered.
    ''' </summary>
    <Theory>
    <InlineData(1, "C3A9")>
    <InlineData(1, "E282AC")>
    <InlineData(2, "E282AC")>
    <InlineData(1, "F09F9880")>
    <InlineData(2, "F09F9880")>
    <InlineData(3, "F09F9880")>
    Public Sub TryGetUtfFileEncodingKind_MultibyteCharacterCutBySampleBoundary_IsStillUtf8(bytesInsideSample As Integer,
                                                                                         characterHex As String)

        Dim character As Byte() = Convert.FromHexString(characterHex)
        Dim bytes(EncodingHelperTests.SampleSize - 1 - bytesInsideSample + character.Length + 100) As Byte

        ' Fill with ASCII, so the character starts at (SampleSize - bytesInsideSample) and is cut by the sample end.
        For index As Integer = 0 To bytes.Length - 1
            bytes(index) = &H61
        Next index
        Buffer.BlockCopy(character, 0, bytes, EncodingHelperTests.SampleSize - bytesInsideSample, character.Length)
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

    ''' <summary>
    ''' A big valid UTF-8 file (several times the sample size) is detected correctly.
    ''' </summary>
    <Fact>
    Public Sub TryGetUtfFileEncodingKind_BigUtf8File_IsUtf8()

        Dim line As String = $"' Línea de comentario con acentos: áéíóú ñ — 日本語{vbLf}"
        Dim builder As New StringBuilder()
        Do While builder.Length < 200000
            builder.Append(line)
        Loop
        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(New UTF8Encoding(False).GetBytes(builder.ToString()), kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

#End Region

#Region " TryGetUtfFileEncodingKind: UTF-7 "

    ''' <summary>
    ''' Text with real UTF-7 shift sequences ("+AOk-" is "é") is UTF-7 and not UTF-8, 
    ''' although it is also valid ASCII/UTF-8 byte-wise.
    ''' </summary>
    <Fact>
    Public Sub TryGetUtfFileEncodingKind_RealUtf7ShiftSequence_IsUtf7()

        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(Encoding.ASCII.GetBytes("Caf+AOk- au lait"), kindName))
        Assert.Equal("UTF7", kindName)

    End Sub

    ''' <summary>
    ''' Ordinary source code that contains plus signs (concatenations, increments, operators) is NOT UTF-7.
    ''' </summary>
    <Theory>
    <InlineData("Dim total As Integer = a + b + c")>
    <InlineData("for (int i = 0; i < n; i++) { total += i; }")>
    <InlineData("Console.WriteLine(""1 + 1 = "" + (1 + 1));")>
    <InlineData("x = y+z;")>
    Public Sub TryGetUtfFileEncodingKind_AsciiCodeWithPlusSigns_IsUtf8(code As String)

        Dim kindName As String = Nothing

        Assert.True(EncodingHelperTests.Detect(Encoding.ASCII.GetBytes(code), kindName))
        Assert.Equal("UTF8", kindName)

    End Sub

#End Region

#Region " TryGetUtfFileEncodingKind: edge cases "

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_EmptyFile_ReturnsFalseAndUnknown()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("empty.txt", Array.Empty(Of Byte)())
            Dim detectedKind As UtfEncodingKind = UtfEncodingKind.UTF8 ' Initialize to something else.

            Dim result As Boolean = EncodingHelper.TryGetUtfFileEncodingKind(filePath, detectedKind)

            Assert.False(result)
            Assert.Equal(UtfEncodingKind.Unknown, detectedKind)

        End Using

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_MissingFile_ThrowsFileNotFoundException()

        Using temp As New TempDirectory()

            Dim missingPath As String = temp.GetPath("missing.txt")
            Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown

            Assert.Throws(Of FileNotFoundException)(
                Function() EncodingHelper.TryGetUtfFileEncodingKind(missingPath, detectedKind))

        End Using

    End Sub

    <Theory>
    <InlineData(Nothing)>
    <InlineData("")>
    <InlineData("   ")>
    Public Sub TryGetUtfFileEncodingKind_NullEmptyOrBlankPath_ThrowsArgumentException(path As String)

        Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown

        ' ArgumentNullException (Nothing) or ArgumentException (empty/blank) depending on the target framework.
        Assert.ThrowsAny(Of ArgumentException)(
            Function() EncodingHelper.TryGetUtfFileEncodingKind(path, detectedKind))

    End Sub

    ''' <summary>
    ''' The detection only reads: the file must be left exactly as it was.
    ''' </summary>
    <Fact>
    Public Sub TryGetUtfFileEncodingKind_DoesNotModifyTheFile()

        Using temp As New TempDirectory()

            Dim original As Byte() = TestEncodings.Encode(SampleText, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True))
            Dim filePath As String = temp.WriteBytes("sample.vb", original)
            Dim stampBefore As DateTime = File.GetLastWriteTimeUtc(filePath)
            Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown

            EncodingHelper.TryGetUtfFileEncodingKind(filePath, detectedKind)

            Assert.Equal(original, File.ReadAllBytes(filePath))
            Assert.Equal(stampBefore, File.GetLastWriteTimeUtc(filePath))

        End Using

    End Sub

    ''' <summary>
    ''' A file that another process (or editor) has open for reading can still be analyzed.
    ''' </summary>
    <Fact>
    Public Sub TryGetUtfFileEncodingKind_FileOpenedForReadingByOthers_CanBeAnalyzed()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("sample.vb", Encoding.ASCII.GetBytes(SampleText))

            Using reader As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

                Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown

                Assert.True(EncodingHelper.TryGetUtfFileEncodingKind(filePath, detectedKind))
                Assert.Equal(UtfEncodingKind.UTF8, detectedKind)

            End Using

        End Using

    End Sub

#End Region

#Region " TryGetUtfFileEncoding "

    <Fact>
    Public Sub TryGetUtfFileEncoding_UnknownEncoding_ReturnsFalseAndNothing()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("latin1.vb", Convert.FromHexString("636166E920746578742E"))
            Dim detectedEncoding As Encoding = Encoding.UTF8 ' Initialize to something else.

            Assert.False(EncodingHelper.TryGetUtfFileEncoding(filePath, detectedEncoding))
            Assert.Null(detectedEncoding)

        End Using

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncoding_EmptyFile_ReturnsFalseAndNothing()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("empty.vb", Array.Empty(Of Byte)())
            Dim detectedEncoding As Encoding = Encoding.UTF8

            Assert.False(EncodingHelper.TryGetUtfFileEncoding(filePath, detectedEncoding))
            Assert.Null(detectedEncoding)

        End Using

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncoding_Utf8Bom_ReturnsAnEncodingThatEmitsTheBom()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("bom.vb", TestEncodings.Encode("x", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)))
            Dim detectedEncoding As Encoding = Nothing

            Assert.True(EncodingHelper.TryGetUtfFileEncoding(filePath, detectedEncoding))
            Assert.Equal("EFBBBF", Convert.ToHexString(detectedEncoding.GetPreamble()))

        End Using

    End Sub

#End Region

#Region " Known Issues "

    <Theory>
    <InlineData("636166E9")>
    <InlineData("636166E90A")>
    <InlineData("2720616CC3")>
    <InlineData("27206361E282")>
    <InlineData("2720F09F98")>
    Public Sub TryGetUtfFileEncodingKind_FileEndingWithATruncatedUtf8Sequence_IsRejected(hexBytes As String)

        Dim kindName As String = Nothing

        Assert.False(EncodingHelperTests.Detect(Convert.FromHexString(hexBytes), kindName))
        Assert.Equal("Unknown", kindName)

    End Sub

    <Fact>
    Public Sub TryGetUtfFileEncodingKind_InvalidUtf8AfterTheSample_IsRejected()

        Dim bytes(EncodingHelperTests.SampleSize + 999) As Byte
        For index As Integer = 0 To bytes.Length - 1
            bytes(index) = &H61
        Next index
        bytes(bytes.Length - 10) = &HE9 ' A Latin-1 "é" far beyond the sample, followed by ASCII.
        Dim kindName As String = Nothing

        Assert.False(EncodingHelperTests.Detect(bytes, kindName))
        Assert.Equal("Unknown", kindName)

    End Sub

#End Region

End Class

#End Region
