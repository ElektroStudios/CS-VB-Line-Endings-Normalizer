#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.IO
Imports System.Text

Imports Xunit

#End Region

#Region " ProgramEndToEndTests "

''' <summary>
''' End-to-end tests of the entry point: the real application is executed as a separate process 
''' against temporary directories, and its exit code, its output and the resulting files are verified.
''' <para></para>
''' They run on Windows only, because the application converts every path to the Windows extended-length format ("\\?\").
''' </summary>
Public Class ProgramEndToEndTests

#Region " Constants and Fields "

    ''' <summary>
    ''' A source file with the three kinds of line breaks and no line break at the end, with non-ASCII text.
    ''' </summary>
    Private Shared ReadOnly MixedText As String = $"' Ñandú café{vbLf}Public Class A{vbCrLf}End Class{vbCr}' end"

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Returns <see cref="MixedText"/> with all its line breaks replaced by the specified sequence.
    ''' </summary>
    Private Shared Function MixedTextWith(lineBreak As String) As String

        Return $"' Ñandú café{lineBreak}Public Class A{lineBreak}End Class{lineBreak}' end"

    End Function

    ''' <summary>
    ''' Gets the line-break sequence of a style, from its command-line name.
    ''' </summary>
    Private Shared Function GetLineBreak(styleName As String) As String

        Select Case styleName.ToUpperInvariant()
            Case "CRLF"
                Return vbCrLf
            Case "LF"
                Return vbLf
            Case "CR"
                Return vbCr
            Case Else
                Throw New ArgumentException($"Unknown style: {styleName}", NameOf(styleName))
        End Select

    End Function

    ''' <summary>
    ''' Asserts that a run ended with the expected exit code, showing the complete output if it did not.
    ''' </summary>
    Private Shared Sub AssertExitCode(expected As Integer, result As ProgramRunResult)

        Assert.True(expected = result.ExitCode, $"Expected exit code {expected}.{Environment.NewLine}{result}")

    End Sub

    ''' <summary>
    ''' Asserts that the output of a run contains a text, showing the complete output if it does not.
    ''' </summary>
    Private Shared Sub AssertOutputContains(expectedText As String, result As ProgramRunResult)

        Assert.True(result.StandardOutput.Contains(expectedText, StringComparison.Ordinal),
                    $"The output does not contain: {expectedText}{Environment.NewLine}{result}")

    End Sub

    ''' <summary>
    ''' Asserts the final counters printed by the application.
    ''' </summary>
    Private Shared Sub AssertSummary(modified As Integer, skipped As Integer, failed As Integer, result As ProgramRunResult)

        ProgramEndToEndTests.AssertOutputContains($"All files have been processed. Modified: {modified}; Skipped: {skipped}; Failed: {failed}.", result)

    End Sub

#End Region

#Region " Arguments And Exit Codes "

    <WindowsOnlyFact>
    Public Sub Run_WithoutArguments_ShowsUsageAndExitsWith1()

        Using sandbox As New ProgramSandbox()

            Dim result As ProgramRunResult = sandbox.RunRaw()

            ProgramEndToEndTests.AssertExitCode(1, result)
            ProgramEndToEndTests.AssertOutputContains("Usage:", result)
            ProgramEndToEndTests.AssertOutputContains("Missing required argument(s)", result)

        End Using

    End Sub

    <WindowsOnlyFact>
    Public Sub Run_WithOnlyTheDirectory_ExitsWith1()

        Using sandbox As New ProgramSandbox()

            Dim result As ProgramRunResult = sandbox.RunRaw(sandbox.Source.FullPath)

            ProgramEndToEndTests.AssertExitCode(1, result)
            ProgramEndToEndTests.AssertOutputContains("Missing required argument(s)", result)

        End Using

    End Sub

    ''' <summary>
    ''' The usage text lists exactly the values of the enumeration, and the exit code is 2.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("UNIX")>
    <InlineData("crlff")>
    <InlineData("")>
    <InlineData("99")>
    <InlineData("-1")>
    <InlineData("-r")>
    Public Sub Run_WithInvalidStyle_ExitsWith2AndListsTheValidValues(styleArgument As String)

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, Encoding.UTF8)

            Dim result As ProgramRunResult = sandbox.Run(styleArgument)

            ProgramEndToEndTests.AssertExitCode(2, result)
            ProgramEndToEndTests.AssertOutputContains("Invalid line ending style", result)
            ProgramEndToEndTests.AssertOutputContains("Valid values are: CRLF, LF, CR", result)

        End Using

    End Sub

    ''' <summary>
    ''' Nothing is modified when the style is invalid.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithInvalidStyle_DoesNotModifyAnyFile()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes("a.vb", original)

            sandbox.Run("nonsense")

            Assert.Equal(original, sandbox.Source.ReadBytes("a.vb"))

        End Using

    End Sub

    <WindowsOnlyFact>
    Public Sub Run_WithMissingDirectory_ExitsWith3()

        Using sandbox As New ProgramSandbox()

            Dim result As ProgramRunResult = sandbox.RunRaw(sandbox.Source.GetPath("does-not-exist"), "CRLF")

            ProgramEndToEndTests.AssertExitCode(3, result)
            ProgramEndToEndTests.AssertOutputContains("The specified directory path does not exist", result)

        End Using

    End Sub

    <WindowsOnlyFact>
    Public Sub Run_WithDirectoryWithoutSourceFiles_ExitsWith4()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("readme.txt", MixedText, Encoding.UTF8)
            sandbox.Source.WriteText("project.vbproj", MixedText, Encoding.UTF8)

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(4, result)
            ProgramEndToEndTests.AssertOutputContains("No supported files were found", result)

        End Using

    End Sub

    <WindowsOnlyFact>
    Public Sub Run_WithEmptyDirectory_ExitsWith4()

        Using sandbox As New ProgramSandbox()

            ProgramEndToEndTests.AssertExitCode(4, sandbox.Run("LF"))

        End Using

    End Sub

    ''' <summary>
    ''' When only the files of a subdirectory exist, they are not found without the recursive switch.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithSourceFilesOnlyInSubdirectory_WithoutRecursion_ExitsWith4()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("sub/a.vb", MixedText, Encoding.UTF8)

            ProgramEndToEndTests.AssertExitCode(4, sandbox.Run("LF"))

        End Using

    End Sub

    ''' <summary>
    ''' The exit code is 0 when everything went fine, and the final line reports the counters.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WhenEverythingWorks_ExitsWith0AndReportsTheCounters()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, Encoding.UTF8)

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(1, 0, 0, result)
            ProgramEndToEndTests.AssertOutputContains("Exiting application with exit code: 0", result)

        End Using

    End Sub

    ''' <summary>
    ''' The directory may be given as a relative path, and may contain spaces and non-ASCII characters.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithRelativePathContainingSpacesAndAccents_Works()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("Mi Carpeta ñ/archivo.vb", MixedText, New UTF8Encoding(False))

            Dim result As ProgramRunResult = ProgramRunner.Run(
                New String() {"Mi Carpeta ñ", "LF"},
                sandbox.Scratch.FullPath,
                workingDirectoryPath:=sandbox.Source.FullPath)

            ProgramEndToEndTests.AssertExitCode(0, result)
            Assert.Equal(MixedTextWith(vbLf), File.ReadAllText(sandbox.Source.GetPath("Mi Carpeta ñ/archivo.vb"), New UTF8Encoding(False)))

        End Using

    End Sub

    ''' <summary>
    ''' A dot as the directory processes the working directory.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithDotAsDirectory_ProcessesTheWorkingDirectory()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.cs", MixedText, New UTF8Encoding(False))

            Dim result As ProgramRunResult = ProgramRunner.Run(
                New String() {".", "CR"},
                sandbox.Scratch.FullPath,
                workingDirectoryPath:=sandbox.Source.FullPath)

            ProgramEndToEndTests.AssertExitCode(0, result)
            Assert.Equal(MixedTextWith(vbCr), File.ReadAllText(sandbox.Source.GetPath("a.cs"), New UTF8Encoding(False)))

        End Using

    End Sub

#End Region

#Region " Conversion "

    ''' <summary>
    ''' Every style converts a file with mixed line breaks, and the style name is not case-sensitive.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("CRLF", "CRLF")>
    <InlineData("LF", "LF")>
    <InlineData("CR", "CR")>
    <InlineData("crlf", "CRLF")>
    <InlineData("Lf", "LF")>
    <InlineData("cR", "CR")>
    Public Sub Run_ConvertsMixedLineBreaksToTheRequestedStyle(styleArgument As String, expectedStyle As String)

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteText("b.cs", MixedText, New UTF8Encoding(False))

            Dim result As ProgramRunResult = sandbox.Run(styleArgument)

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(2, 0, 0, result)

            Dim expected As String = MixedTextWith(GetLineBreak(expectedStyle))
            Assert.Equal(expected, File.ReadAllText(sandbox.Source.GetPath("a.vb"), New UTF8Encoding(False)))
            Assert.Equal(expected, File.ReadAllText(sandbox.Source.GetPath("b.cs"), New UTF8Encoding(False)))

        End Using

    End Sub

    ''' <summary>
    ''' A file keeps having no line break at its end if it had none, and keeps it if it had one.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_PreservesTheAbsenceOrPresenceOfTheFinalLineBreak()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteBytes("without.vb", Encoding.ASCII.GetBytes($"a{vbLf}b"))
            sandbox.Source.WriteBytes("with.vb", Encoding.ASCII.GetBytes($"a{vbLf}b{vbLf}"))

            sandbox.Run("CRLF")

            Assert.Equal($"a{vbCrLf}b", Encoding.ASCII.GetString(sandbox.Source.ReadBytes("without.vb")))
            Assert.Equal($"a{vbCrLf}b{vbCrLf}", Encoding.ASCII.GetString(sandbox.Source.ReadBytes("with.vb")))

        End Using

    End Sub

    ''' <summary>
    ''' Running the application twice gives the same result: the second run has nothing to modify.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_IsIdempotent()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, New UTF8Encoding(False))

            sandbox.Run("LF")
            Dim afterFirst As Byte() = sandbox.Source.ReadBytes("a.vb")
            Dim second As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertExitCode(0, second)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, second)
            Assert.Equal(afterFirst, sandbox.Source.ReadBytes("a.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' The conversion can be chained from one style to another and back without losing anything.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_ChainedStyles_RoundTripToTheOriginalContent()

        Using sandbox As New ProgramSandbox()

            Dim crlfText As String = MixedTextWith(vbCrLf)
            sandbox.Source.WriteText("a.vb", crlfText, New UTF8Encoding(False))

            sandbox.Run("LF")
            sandbox.Run("CR")
            sandbox.Run("CRLF")

            Assert.Equal(crlfText, File.ReadAllText(sandbox.Source.GetPath("a.vb"), New UTF8Encoding(False)))

        End Using

    End Sub

    ''' <summary>
    ''' The counters of the header show how many files of each language were found.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_ReportsTheNumberOfFilesFoundPerLanguage()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.cs", MixedText, Encoding.UTF8)
            sandbox.Source.WriteText("b.cs", MixedText, Encoding.UTF8)
            sandbox.Source.WriteText("c.vb", MixedText, Encoding.UTF8)
            sandbox.Source.WriteText("ignored.txt", MixedText, Encoding.UTF8)

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertOutputContains("3 source-code files (*.cs: 2, *.vb: 1)", result)

        End Using

    End Sub

    ''' <summary>
    ''' The files that are not C# or VB files are never touched.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_DoesNotTouchOtherFileTypes()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            For Each fileName As String In New String() {"readme.txt", "app.vbproj", "app.csproj", "page.cshtml", "script.vbs", "data.json"}
                sandbox.Source.WriteBytes(fileName, original)
            Next fileName
            sandbox.Source.WriteBytes("real.cs", original)

            sandbox.Run("CRLF")

            For Each fileName As String In New String() {"readme.txt", "app.vbproj", "app.csproj", "page.cshtml", "script.vbs", "data.json"}
                Assert.Equal(original, sandbox.Source.ReadBytes(fileName))
            Next fileName
            Assert.NotEqual(original, sandbox.Source.ReadBytes("real.cs"))

        End Using

    End Sub

#End Region

#Region " Recursion "

    <WindowsOnlyFact>
    Public Sub Run_WithoutRecursiveSwitch_ProcessesOnlyTheRootDirectory()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes("root.vb", original)
            sandbox.Source.WriteBytes("sub/child.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            Assert.NotEqual(original, sandbox.Source.ReadBytes("root.vb"))
            Assert.Equal(original, sandbox.Source.ReadBytes("sub/child.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' Both spellings of the switch, in any case and in any position after the style, process every subdirectory.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("-r")>
    <InlineData("-R")>
    <InlineData("--recursive")>
    <InlineData("--RECURSIVE")>
    Public Sub Run_WithRecursiveSwitch_ProcessesEverySubdirectory(recursiveSwitch As String)

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes("root.vb", original)
            sandbox.Source.WriteBytes("sub/child.cs", original)
            sandbox.Source.WriteBytes("sub/deeper/grandchild.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("LF", recursiveSwitch)

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(3, 0, 0, result)
            For Each relativePath As String In New String() {"root.vb", "sub/child.cs", "sub/deeper/grandchild.vb"}
                Assert.NotEqual(original, sandbox.Source.ReadBytes(relativePath))
            Next relativePath

        End Using

    End Sub

    ''' <summary>
    ''' The switches are found wherever they are after the mandatory arguments, and several switches can be combined.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithRecursiveAndTestSwitches_Combined_SimulatesTheWholeTree()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes("root.vb", original)
            sandbox.Source.WriteBytes("sub/child.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("CRLF", "--test", "-r")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(2, 0, 0, result)
            Assert.Equal(original, sandbox.Source.ReadBytes("root.vb"))
            Assert.Equal(original, sandbox.Source.ReadBytes("sub/child.vb"))

        End Using

    End Sub

#End Region

#Region " Test Mode "

    ''' <summary>
    ''' In test mode (dry-run) no file is modified, the run is still reported as successful, 
    ''' and no temporary or backup file is left behind, neither next to the sources nor in the temporary directory.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("-t")>
    <InlineData("--test")>
    <InlineData("-T")>
    Public Sub Run_InTestMode_DoesNotModifyAnyFileAndLeavesNoTraces(testSwitch As String)

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(True))
            sandbox.Source.WriteBytes("a.vb", original)
            sandbox.Source.WriteBytes("b.cs", original)

            Dim result As ProgramRunResult = sandbox.Run("LF", testSwitch)

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(2, 0, 0, result)
            ProgramEndToEndTests.AssertOutputContains("TEST MODE IS ENABLED (NO FILES HAVE BEEN MODIFIED)", result)
            ProgramEndToEndTests.AssertOutputContains("(Test Mode)", result)
            Assert.Equal(original, sandbox.Source.ReadBytes("a.vb"))
            Assert.Equal(original, sandbox.Source.ReadBytes("b.cs"))
            Assert.Equal(New String() {"a.vb", "b.cs"}, sandbox.Source.GetAllFileNames())
            Assert.Empty(sandbox.Scratch.GetAllFileNames())

        End Using

    End Sub

    ''' <summary>
    ''' Without test mode the final line does not talk about test mode.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WithoutTestMode_DoesNotMentionTestMode()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, Encoding.UTF8)

            Dim result As ProgramRunResult = sandbox.Run("LF")

            Assert.DoesNotContain("TEST MODE", result.StandardOutput, StringComparison.Ordinal)
            Assert.Matches("Test Mode Enabled\s*: False", result.StandardOutput)

        End Using

    End Sub

#End Region

#Region " Encodings "

    ''' <summary>
    ''' Every supported encoding variant is rewritten with the same encoding, the same BOM (or absence of BOM) 
    ''' and exactly the converted text. The comparison is made on the raw bytes.
    ''' </summary>
    <WindowsOnlyTheory>
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
    Public Sub Run_PreservesTheEncodingAndTheBomOfTheFile(encodingName As String)

        Using sandbox As New ProgramSandbox()

            Dim encoding As Encoding = TestEncodings.Create(encodingName)
            sandbox.Source.WriteBytes("a.vb", TestEncodings.Encode(MixedText, encoding))

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(1, 0, 0, result)
            Assert.Equal(TestEncodings.Encode(MixedTextWith(vbCrLf), encoding), sandbox.Source.ReadBytes("a.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' Text outside the Basic Multilingual Plane (emoji) and CJK text survive the rewrite in every encoding that has a BOM.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("UTF8")>
    <InlineData("UTF8_Bom")>
    <InlineData("UTF16LE_Bom")>
    <InlineData("UTF16BE_Bom")>
    <InlineData("UTF32LE_Bom")>
    <InlineData("UTF32BE_Bom")>
    Public Sub Run_PreservesCjkAndSurrogatePairs(encodingName As String)

        Using sandbox As New ProgramSandbox()

            Dim emoji As String = $"{ChrW(&HD83D)}{ChrW(&HDE00)}"
            Dim original As String = $"' 日本語 {emoji}{vbLf}Dim x As String = ""é{emoji}""{vbLf}"
            Dim expected As String = $"' 日本語 {emoji}{vbCrLf}Dim x As String = ""é{emoji}""{vbCrLf}"
            Dim encoding As Encoding = TestEncodings.Create(encodingName)
            sandbox.Source.WriteBytes("a.vb", TestEncodings.Encode(original, encoding))

            ProgramEndToEndTests.AssertExitCode(0, sandbox.Run("CRLF"))

            Assert.Equal(TestEncodings.Encode(expected, encoding), sandbox.Source.ReadBytes("a.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' A file in a legacy encoding (Latin-1 / Windows-1252) is skipped and left byte-for-byte untouched.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_FileInLegacyEncoding_IsSkippedAndNotModified()

        Using sandbox As New ProgramSandbox()

            ' "' caf" + 0xE9 + CR LF + "x" + LF  (an 'é' in Latin-1, followed by text and mixed line breaks).
            Dim original As Byte() = Convert.FromHexString("2720636166E90D0A780A")
            sandbox.Source.WriteBytes("legacy.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            ProgramEndToEndTests.AssertOutputContains("Unsupported or unknown file encoding", result)
            Assert.Equal(original, sandbox.Source.ReadBytes("legacy.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' UTF-7 files are detected and skipped on purpose (the encoding is obsolete and rewriting it is not safe).
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_Utf7File_IsSkippedAndNotModified()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = Encoding.ASCII.GetBytes($"' Caf+AOk- au lait{vbCrLf}x{vbLf}")
            sandbox.Source.WriteBytes("utf7.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            ProgramEndToEndTests.AssertOutputContains("Unsupported or unknown file encoding", result)
            Assert.Equal(original, sandbox.Source.ReadBytes("utf7.vb"))

        End Using

    End Sub

#End Region

#Region " Skipped Files "

    ''' <summary>
    ''' The files generated by Visual Studio (designer files, assembly attributes, AssemblyInfo) are skipped, in any case.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("Form1.Designer.vb")>
    <InlineData("Form1.Designer.cs")>
    <InlineData("Form1.DESIGNER.vb")>
    <InlineData("Project.AssemblyAttributes.cs")>
    <InlineData("AssemblyInfo.vb")>
    <InlineData("AssemblyInfo.cs")>
    <InlineData("My Project/AssemblyInfo.vb")>
    Public Sub Run_GeneratedFiles_AreSkippedAndNotModified(relativePath As String)

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes(relativePath, original)

            Dim result As ProgramRunResult = sandbox.Run("LF", "-r")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            ProgramEndToEndTests.AssertOutputContains("matches a prohibited pattern", result)
            Assert.Equal(original, sandbox.Source.ReadBytes(relativePath))

        End Using

    End Sub

    ''' <summary>
    ''' Names that merely look similar to a generated file are processed.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("Designer.vb")>
    <InlineData("MyDesigner.cs")>
    <InlineData("AssemblyInfoHelper.cs")>
    <InlineData("Form1.Designers.vb")>
    Public Sub Run_FilesWithSimilarNames_AreProcessed(fileName As String)

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText(fileName, MixedText, New UTF8Encoding(False))

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertSummary(1, 0, 0, result)

        End Using

    End Sub

    ''' <summary>
    ''' Empty files and files that contain only white space are skipped, even if the white space includes line breaks.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData("\r\n\r\n\r\n")>
    <InlineData("\n\n")>
    <InlineData("\t \r \n")>
    Public Sub Run_EmptyOrWhitespaceOnlyFiles_AreSkippedAndNotModified(escapedContent As String)

        Using sandbox As New ProgramSandbox()

            Dim content As String = escapedContent.Replace("\r", vbCr).Replace("\n", vbLf).Replace("\t", vbTab)
            Dim original As Byte() = TestEncodings.Encode(content, New UTF8Encoding(True)) ' With a BOM, so an empty text is not an empty file.
            sandbox.Source.WriteBytes("blank.vb", original)

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            Assert.Equal(original, sandbox.Source.ReadBytes("blank.vb"))

        End Using

    End Sub

    <WindowsOnlyFact>
    Public Sub Run_ZeroByteFile_IsSkippedAndNotModified()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteBytes("zero.vb", Array.Empty(Of Byte)())

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            Assert.Empty(sandbox.Source.ReadBytes("zero.vb"))

        End Using

    End Sub

    ''' <summary>
    ''' A file that already has the requested style is neither rewritten nor touched (not even its modification time).
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_FileAlreadyInTheRequestedStyle_IsNotRewritten()

        Using sandbox As New ProgramSandbox()

            Dim filePath As String = sandbox.Source.WriteText("ok.vb", MixedTextWith(vbCrLf), New UTF8Encoding(True))
            Dim oldStamp As New DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc)
            File.SetLastWriteTimeUtc(filePath, oldStamp)

            Dim result As ProgramRunResult = sandbox.Run("CRLF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(0, 1, 0, result)
            ProgramEndToEndTests.AssertOutputContains("already match the target style (CRLF)", result)
            Assert.Equal(oldStamp, File.GetLastWriteTimeUtc(filePath))

        End Using

    End Sub

    ''' <summary>
    ''' A run with a mixture of everything reports the right counters: 2 modified, 3 skipped, 0 failed.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_MixedDirectory_ReportsModifiedAndSkippedCounters()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("modify1.vb", MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteText("modify2.cs", MixedText, New UTF8Encoding(True))
            sandbox.Source.WriteText("already.vb", MixedTextWith(vbLf), New UTF8Encoding(False))
            sandbox.Source.WriteText("Form1.Designer.vb", MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteBytes("legacy.cs", Convert.FromHexString("2F2F636166E90A780D"))

            Dim result As ProgramRunResult = sandbox.Run("LF")

            ProgramEndToEndTests.AssertExitCode(0, result)
            ProgramEndToEndTests.AssertSummary(2, 3, 0, result)

        End Using

    End Sub

#End Region

#Region " Failures "

    ''' <summary>
    ''' A file that cannot be processed (here, it is locked by another process) makes the run end with exit code 1,
    ''' but it does not stop the processing of the rest of the files, and the locked file is left intact.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_WhenAFileCannotBeProcessed_ReportsItContinuesAndExitsWith1()

        Using sandbox As New ProgramSandbox()

            Dim original As Byte() = TestEncodings.Encode(MixedText, New UTF8Encoding(False))
            Dim lockedPath As String = sandbox.Source.WriteBytes("a_locked.vb", original)
            sandbox.Source.WriteBytes("b_free.vb", original)

            Dim result As ProgramRunResult
            Using locker As New FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                result = sandbox.Run("LF")
            End Using

            ProgramEndToEndTests.AssertExitCode(1, result)
            ProgramEndToEndTests.AssertSummary(1, 0, 1, result)
            ProgramEndToEndTests.AssertOutputContains("An error occurred while processing the file", result)
            Assert.Equal(original, sandbox.Source.ReadBytes("a_locked.vb"))
            Assert.NotEqual(original, sandbox.Source.ReadBytes("b_free.vb"))

        End Using

    End Sub

#End Region

#Region " Clean Up "

    ''' <summary>
    ''' After a real run, only the original files are in the directory: no temporary (.tmp) or backup (.bak) files remain.
    ''' </summary>
    <WindowsOnlyFact>
    Public Sub Run_LeavesNoTemporaryOrBackupFilesBehind()

        Using sandbox As New ProgramSandbox()

            sandbox.Source.WriteText("a.vb", MixedText, New UTF8Encoding(False))
            sandbox.Source.WriteText("sub/b.cs", MixedText, New UTF8Encoding(True))
            sandbox.Source.WriteText("c.vb", MixedTextWith(vbLf), New UTF8Encoding(False)) ' Already converted: skipped.

            Dim result As ProgramRunResult = sandbox.Run("LF", "-r")

            ProgramEndToEndTests.AssertExitCode(0, result)
            Assert.Equal(New String() {"a.vb", "c.vb", "sub/b.cs"}, sandbox.Source.GetAllFileNames())
            Assert.Empty(sandbox.Scratch.GetAllFileNames())

        End Using

    End Sub

#End Region

#Region " Other "

    <Fact>
    Public Sub Run_ExitMessage_ShowsTheExitCodeAsEightDigitHexadecimal()

        Using sandbox As New ProgramSandbox()

            Dim result As ProgramRunResult = sandbox.RunRaw(sandbox.Source.GetPath("does-not-exist"), "CRLF")

            ProgramEndToEndTests.AssertOutputContains("Exiting application with exit code: 3 (0x3)", result)

        End Using

    End Sub

    <Fact>
    Public Sub Run_LegacyFileEndingWithAnAccentedLetter_IsSkippedAndNotModified()

        Using sandbox As New ProgramSandbox()

            ' "' Linea 1" CR LF "Dim cafe = 1" LF "' caf" 0xE9 (no final line break)
            Dim original As Byte() = Convert.FromHexString("27204C696E656120310D0A44696D2063616665203D20310A2720636166E9")
            sandbox.Source.WriteBytes("legacy.vb", original)

            ProgramEndToEndTests.AssertExitCode(0, sandbox.Run("LF"))

            Assert.Equal(original, sandbox.Source.ReadBytes("legacy.vb"))

        End Using

    End Sub

    <Fact>
    Public Sub Run_BigLegacyFileWithAccentsAfterTheSample_IsSkippedAndNotModified()

        Using sandbox As New ProgramSandbox()

            Dim builder As New StringBuilder()
            For lineNumber As Integer = 1 To 2750
                builder.Append($"' ascii line{vbCrLf}")
            Next lineNumber
            Dim original As Byte() = Encoding.Latin1.GetBytes($"{builder}' café ñandú{vbCrLf}' end{vbCrLf}")
            sandbox.Source.WriteBytes("legacy-big.vb", original)

            ProgramEndToEndTests.AssertExitCode(0, sandbox.Run("LF"))

            Assert.Equal(original, sandbox.Source.ReadBytes("legacy-big.vb"))

        End Using

    End Sub

#End Region

End Class

#End Region
