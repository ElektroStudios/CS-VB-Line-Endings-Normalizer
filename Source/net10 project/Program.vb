
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Threading

#If Not NETCOREAPP Then
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Imports DevCase.Runtime.TypeComparers
#End If

#End Region

Public Module Program

#Region " Constants and Fields "

    ''' <summary>
    ''' The file extension for supported C# source-code files. Only C# files with this extension will be processed.
    ''' </summary>
    Friend Const CsFileExtension As String = ".cs"

    ''' <summary>
    ''' The file extension for supported VB.NET source-code files. Only VB.NET files with this extension will be processed.
    ''' </summary>
    Friend Const VbFileExtension As String = ".vb"

    ''' <summary>
    ''' The set of C# and VB.NET source-code file name patterns to ignore during processing. 
    ''' <para></para>
    ''' Any file whose name contains any of these patterns (case-insensitive) will be skipped.
    ''' <para></para>
    ''' Use cases for ignoring files that shouldn't be modified include:
    ''' <para></para>
    '''   - Auto-generated designer files (e.g., Form1.Designer.cs, Form1.Designer.vb).
    ''' <para></para>
    '''   - Auto-generated assembly attribute files (i.e., *.AssemblyAttributes.cs, *.AssemblyAttributes.vb).
    ''' <para></para>
    '''   - Assembly metadata files (i.e., AssemblyInfo.cs, AssemblyInfo.vb).
    ''' </summary>
    Private ReadOnly ProhibitedFileNamePatterns As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        ".Designer",
        ".AssemblyAttributes",
        "AssemblyInfo"
    }

#If NETCOREAPP Then
    ''' <summary>
    ''' The suffix used for temporary files created during the atomic file replacement process.
    ''' </summary>
    Private ReadOnly GeneratedTempFileSuffix As String = $"{Assembly.GetEntryAssembly().GetName().Name}.tmp"

    ''' <summary>
    ''' The suffix used for backup files created during the atomic file replacement process.
    ''' </summary>
    Private ReadOnly GeneratedBackupFileSuffix As String = $"{Assembly.GetEntryAssembly().GetName().Name}.bak"
#Else
    ''' <summary>
    ''' The suffix used for temporary files created during the atomic file replacement process.
    ''' </summary>
    Private ReadOnly GeneratedTempFileSuffix As String = $"{My.Application.Info.AssemblyName}.tmp"

    ''' <summary>
    ''' The suffix used for backup files created during the atomic file replacement process.
    ''' </summary>
    Private ReadOnly GeneratedBackupFileSuffix As String = $"{My.Application.Info.AssemblyName}.bak"
#End If

    ''' <summary>
    ''' The <see cref="CultureInfo"/> instance representing the "en-US" culture.
    ''' </summary>
    Private ReadOnly CultureInfoEnUs As New CultureInfo("en-US")

    ''' <summary>
    ''' The UTF-8 encoding instance used for console output, configured to not emit a BOM (Byte Order Mark).
    ''' </summary>
    Private ReadOnly ConsoleEncoding As New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)

#End Region

#Region " Entry Point "

    ''' <summary>
    ''' The main entry point of the application.
    ''' </summary>
    ''' 
    ''' <param name="args">
    ''' The command-line arguments passed to the application.
    ''' <para></para>
    ''' The first argument (args(0)) is expected to be the path to the 
    ''' source directory containing VB and/or CS files to normalize.
    ''' </param>
    <DebuggerStepperBoundary>
    Public Sub Main(args As String())

        Thread.CurrentThread.CurrentCulture = Program.CultureInfoEnUs
        Thread.CurrentThread.CurrentUICulture = Program.CultureInfoEnUs

        Console.OutputEncoding = Program.ConsoleEncoding
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

#If NETCOREAPP Then
        Dim versionInfo As FileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath)
        Dim version As String = versionInfo.ProductVersion
        Dim assemblyTitle As String = versionInfo.FileDescription
#Else
        Dim version As String = My.Application.Info.Version.ToString(fieldCount:=3)
        Dim assemblyTitle As String = My.Application.Info.Title
#End If

        Dim consoletitle As String = $"{assemblyTitle} {version} ─ by ElektroStudios"
#If DEBUG Then
        Console.Title = consoletitle
#End If
        ConsoleHelper.WriteColoredTextLine(" " & consoletitle, ConsoleColor.Cyan)
        Console.WriteLine("╭─────────────────────────────────────────────────────────────────────────────────────────╮")
        Console.WriteLine("│ Purpose:                                                                                │")
        Console.WriteLine("│   This application normalizes line endings (CRLF / CR / LF) in your CS and VB files.    │")
        Console.WriteLine("│                                                                                         │")
        Console.WriteLine("│   It scans the given directory, processing all supported source files and overwriting   │")
        Console.WriteLine("│   them directly with the requested canonical line ending style to ensure consistency.   │")
        Console.WriteLine("│                                                                                         │")
        Console.WriteLine("│   Source files are read and rewritten preserving their exact UTF encoding structure,    │")
        Console.WriteLine("│   supporting UTF-8, UTF-16, and UTF-32, either with or without a Byte Order Mark (BOM). │")
        Console.WriteLine("│                                                                                         │")
        Console.WriteLine("│   To prevent data corruption, files with unknown encodings are automatically skipped.   │")
        Console.WriteLine("│                                                                                         │")
        Console.WriteLine("│ [!] Disclaimer:                                                                         │")
        Console.WriteLine("│   This program is distributed 'as-is', without any warranty; Use it at your own risk.   │")
        Console.WriteLine("│   Ensure to make a backup of your CS/VB files before running this application.          │")
        Console.WriteLine("╰─────────────────────────────────────────────────────────────────────────────────────────╯")
        Console.WriteLine()

        If args.Length < 2 Then
            Program.ShowUsage()
            ConsoleHelper.ExitWithMessage("[ ERROR ] Missing required argument(s). See usage above.", exitCode:=1, ConsoleColor.Red)
        End If

        Dim isTestMode As Boolean =
            args.Contains("-t", StringComparer.OrdinalIgnoreCase) OrElse
            args.Contains("--test", StringComparer.OrdinalIgnoreCase)

        Dim isRecursiveSearch As Boolean =
            args.Contains("-r", StringComparer.OrdinalIgnoreCase) OrElse
            args.Contains("--recursive", StringComparer.OrdinalIgnoreCase)

        Dim currentSearchOption As SearchOption = If(isRecursiveSearch, SearchOption.AllDirectories, SearchOption.TopDirectoryOnly)

        ' Parse and validate the mandatory line ending style argument (args(1)).
        Dim lineEndingStyleArg As String = args(1)
        Dim targetLineEndingStyle As LineEndingStyle = LineEndingStyle.CRLF

#If NETCOREAPP Then
        Dim lineEndingStyleIsValid As Boolean =
            [Enum].TryParse(lineEndingStyleArg, ignoreCase:=True, result:=targetLineEndingStyle) AndAlso
            [Enum].IsDefined(targetLineEndingStyle)
#Else
        Dim lineEndingStyleIsValid As Boolean =
            [Enum].TryParse(lineEndingStyleArg, ignoreCase:=True, result:=targetLineEndingStyle) AndAlso
            [Enum].IsDefined(GetType(LineEndingStyle), targetLineEndingStyle)
#End If

        If Not lineEndingStyleIsValid Then
#If NETCOREAPP Then
            Dim validValues As String = String.Join(", ", [Enum].GetNames(Of LineEndingStyle)())
#Else
            Dim validValues As String = String.Join(", ", [Enum].GetNames(GetType(LineEndingStyle)))
#End If
            Program.ShowUsage()
            ConsoleHelper.ExitWithMessage($"[ ERROR ] Invalid line ending style: ""{lineEndingStyleArg}"". Valid values are: {validValues}", exitCode:=2, ConsoleColor.Red)
        End If

        Dim totalUpdatedFiles As Integer = 0
        Dim totalSkippedFiles As Integer = 0
        Dim totalFailedFiles As Integer = 0

        Dim sourceDirPath As String = args(0)

        ConsoleHelper.WriteColoredTextLine($"Gathering files from specified directory path...", ConsoleColor.Cyan)
        Console.WriteLine()
        Try
            ' Convert to absolute path first and then apply the Long Path prefix if required.
            Dim sourceDirPathExtended As String = Path.GetFullPath(sourceDirPath)
            sourceDirPathExtended = FileSystemHelper.GetExtendedPath(sourceDirPathExtended)

            If Not Directory.Exists(sourceDirPath) Then
                Dim exitMsg As String = $"[ ERROR ] The specified directory path does not exist: {sourceDirPath}"
                ConsoleHelper.ExitWithMessage(exitMsg, exitCode:=3, ConsoleColor.Red)
            End If

#If NETCOREAPP Then
            Dim filePathComparer As StringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering)
#Else
            Dim filePathComparer As New StringNaturalComparer()
#End If

            Dim sourceFiles As New SortedSet(Of String)(
                FileSystemHelper.EnumerateSourceFiles(sourceDirPathExtended, currentSearchOption), filePathComparer)

            Dim totalCsFiles As Integer = 0
            Dim totalVbFiles As Integer = 0

            For Each filePath As String In sourceFiles

                Dim extension As String = Path.GetExtension(filePath)
                If extension.Equals(Program.CsFileExtension, StringComparison.OrdinalIgnoreCase) Then
                    totalCsFiles += 1

                ElseIf extension.Equals(Program.VbFileExtension, StringComparison.OrdinalIgnoreCase) Then
                    totalVbFiles += 1

                End If

#If DEBUG Then
                Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
#End If
            Next filePath

            Dim totalSourceFileCount As Integer =
                If(sourceFiles Is Nothing, 0, sourceFiles.Count)

            If totalSourceFileCount = 0 Then
                Dim exitMsg As String = $"[ ERROR ] No supported files were found in source directory: {sourceDirPath}"
                ConsoleHelper.ExitWithMessage(exitMsg, exitCode:=4, ConsoleColor.Red)
            End If

            ConsoleHelper.WriteColoredTextLine($"Source Directory Path    : {sourceDirPath}", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredTextLine($"Search Option            : {currentSearchOption}", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredTextLine($"Supported Files Found    : {totalSourceFileCount:N0} source-code files (*.cs: {totalCsFiles:N0}, *.vb: {totalVbFiles:N0})", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredTextLine($"Target Line Ending Style : {targetLineEndingStyle}", ConsoleColor.DarkCyan)
            ConsoleHelper.WriteColoredTextLine($"Test Mode Enabled        : {isTestMode}", ConsoleColor.DarkCyan)
            Console.WriteLine()

#If DEBUG Then
            ConsoleHelper.WriteColoredTextLine("Press 'Y' key to start processing CS/VB files, or 'Escape' key to exit...", ConsoleColor.Yellow)
            ConsoleHelper.WriteColoredTextLine("[!] This message only appears in DEBUG mode to prevent accidental execution.", ConsoleColor.Yellow)
            Do
                Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(intercept:=True)
                If keyInfo.Key = ConsoleKey.Y Then
                    Exit Do
                ElseIf keyInfo.Key = ConsoleKey.Escape Then
                    Environment.Exit(0)
                End If
            Loop
            Console.WriteLine()
#End If

            ' Iterate through each source file and normalize line endings.
            For i As Integer = 0 To totalSourceFileCount - 1

                Dim currentFile As String = sourceFiles(i)
                ConsoleHelper.WriteColoredTextLine($"[{i + 1:N0} of {totalSourceFileCount:N0}] Processing file: {FileSystemHelper.GetNormalPath(currentFile)} ...", ConsoleColor.Cyan)

                Try
                    Dim fileName As String = Path.GetFileName(currentFile)
                    Dim fileExtension As String = Path.GetExtension(fileName)
#If NETCOREAPP Then
                    Dim prohibitedFileNameMatchedPattern As String =
                        Program.ProhibitedFileNamePatterns.FirstOrDefault(
                            Function(pattern As String) fileName.Contains($"{pattern}{fileExtension}", StringComparison.OrdinalIgnoreCase))
#Else
                    Dim prohibitedFileNameMatchedPattern As String =
                        Program.ProhibitedFileNamePatterns.FirstOrDefault(
                            Function(pattern As String) fileName.IndexOf($"{pattern}{fileExtension}", StringComparison.OrdinalIgnoreCase) >= 0)
#End If

                    If prohibitedFileNameMatchedPattern IsNot Nothing Then
                        ConsoleHelper.WriteColoredTextLine($"[SKIPPED] The file name matches a prohibited pattern: ""{prohibitedFileNameMatchedPattern}{fileExtension}""", ConsoleColor.DarkYellow)
                        Console.WriteLine()
                        totalSkippedFiles += 1
                        Continue For
                    End If

                    Dim detectedEncodingKind As UtfEncodingKind = UtfEncodingKind.Unknown
                    If Not EncodingHelper.TryGetUtfFileEncodingKind(currentFile, detectedEncodingKind) OrElse
                           detectedEncodingKind = UtfEncodingKind.UTF7 Then
#If DEBUG Then
                        ConsoleHelper.WriteColoredTextLine($"[ DEBUG ] Detected encoding kind: {detectedEncodingKind}", ConsoleColor.Magenta)
#End If
                        ConsoleHelper.WriteColoredTextLine($"[SKIPPED] Unsupported or unknown file encoding.", ConsoleColor.Yellow)
                        Console.WriteLine()
                        totalSkippedFiles += 1
                        Continue For
                    End If

                    Dim detectedEncoding As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(detectedEncodingKind)
#If DEBUG Then
                    Dim hasBOM As Boolean = detectedEncoding.GetPreamble().Length > 0
                    ConsoleHelper.WriteColoredTextLine($"[ DEBUG ] Detected encoding: {detectedEncoding.BodyName}{If(hasBOM, " (with BOM)", "")}", ConsoleColor.Magenta)
#End If

                    Dim sourceCode As String = File.ReadAllText(currentFile, detectedEncoding)

                    If String.IsNullOrWhiteSpace(sourceCode) Then
                        ConsoleHelper.WriteColoredTextLine($"[SKIPPED] The file is empty or contains only white-spaces.", ConsoleColor.DarkGray)
                        Console.WriteLine()
                        totalSkippedFiles += 1
                        Continue For
                    End If

                    Dim normalizedCode As String = StringsHelper.NormalizeLineEndings(sourceCode, targetLineEndingStyle)

                    Dim lineEndingsAlreadyNormalized As Boolean = sourceCode.Equals(normalizedCode, StringComparison.Ordinal)
                    If lineEndingsAlreadyNormalized Then
                        ConsoleHelper.WriteColoredTextLine($"[SKIPPED] Line endings already match the target style ({targetLineEndingStyle})", ConsoleColor.DarkGray)
                        Console.WriteLine()
                        totalSkippedFiles += 1
                        Continue For
                    End If

                    ' Create the temporary file in the SAME directory than the current file is, 
                    ' and use File.Replace() to ensure an atomic replacement operation.
                    Dim destDirectory As String = If(isTestMode,
                                                     Path.GetTempPath(),
                                                     Path.GetDirectoryName(currentFile))

                    Dim destFilename As String = Path.GetFileName(currentFile)
                    If Not currentFile.EndsWith(destFilename, StringComparison.Ordinal) Then
                        Dim errMsg As String =
                            $"[ ERROR ] Failed to extract the destination filename from the current file path." & Environment.NewLine &
                            $"          Original filepath : ""{currentFile}""" & Environment.NewLine &
                            $"          Returned filename : ""{destFilename}""" & Environment.NewLine &
                             "          Operation aborted, no changes have been made to the file."

                        ConsoleHelper.WriteColoredTextLine(errMsg, ConsoleColor.Red)
                        Console.WriteLine()
                        totalFailedFiles += 1
                        Continue For
                    End If

                    Dim tempFileName As String = $"{destFilename}.{Program.GeneratedTempFileSuffix}"
                    Dim bakFileName As String = $"{destFilename}.{Program.GeneratedBackupFileSuffix}"
                    Dim tempFilePath As String = FileSystemHelper.GetExtendedPath(Path.Combine(destDirectory, tempFileName))
                    Dim bakFilePath As String = FileSystemHelper.GetExtendedPath(Path.Combine(destDirectory, bakFileName))

#If DEBUG Then
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Is Test Mode   : " & isTestMode, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Current File   : " & currentFile, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Dest Directory : " & destDirectory, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Dest Filename  : " & destFilename, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Temp Filename  : " & tempFileName, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Bak  Filename  : " & bakFileName, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Temp Filepath  : " & tempFilePath, ConsoleColor.Magenta)
                    ConsoleHelper.WriteColoredTextLine("[ DEBUG ] Bak  Filepath  : " & bakFilePath, ConsoleColor.Magenta)
#End If

                    ConsoleHelper.WriteColoredTextLine($"[ INFO  ] Writing changes to temporary file...", ConsoleColor.Gray)
                    File.WriteAllText(tempFilePath, normalizedCode, detectedEncoding)

                    Try
                        ConsoleHelper.WriteColoredTextLine($"[ INFO  ] Replacing original file with temporary one...", ConsoleColor.Gray)
                        If Not isTestMode Then
                            File.Replace(tempFilePath, currentFile, bakFilePath)
                        End If

                    Catch ex As Exception
                        Dim errMsg As String =
                            $"[ ERROR ] An error occurred while replacing the file. Error code: {ex.HResult} (0x{ex.HResult:X8}). Error message: " & Environment.NewLine &
                            $"          {ex.Message}>" & Environment.NewLine &
                             "          Please, ensure that the file not get corrupted." & Environment.NewLine &
                             "          A backup of the unmodified original file may be available at: " & bakFilePath

                        ConsoleHelper.WriteColoredTextLine(errMsg, ConsoleColor.Red)
                        Console.WriteLine()
                        totalFailedFiles += 1
                        Continue For
                    End Try

                    If File.Exists(bakFilePath) Then
                        File.Delete(bakFilePath)
                    End If

                    If isTestMode Then
                        If File.Exists(tempFilePath) Then
                            File.Delete(tempFilePath)
                        End If
                    End If

                    ConsoleHelper.WriteColoredTextLine($"[SUCCESS] Line endings were normalized to {targetLineEndingStyle}{If(isTestMode, " (Test Mode)", "")}", ConsoleColor.Green)
                    Console.WriteLine()
                    totalUpdatedFiles += 1

                Catch ex As Exception
                    Dim errMsg As String =
                        $"[ ERROR ] An error occurred while processing the file. Error code: {ex.HResult} (0x{ex.HResult:X8}). Error message: " & Environment.NewLine &
                        $"          {ex.Message}>" & Environment.NewLine &
                         "          Operation aborted, no changes have been made to the file."

                    ConsoleHelper.WriteColoredTextLine(errMsg, ConsoleColor.Red)
                    Console.WriteLine()
                    totalFailedFiles += 1
                    Continue For

                Finally
#If DEBUG Then
                    Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
#End If
                End Try

            Next i

        Catch ex As Exception
            Console.WriteLine()
            ConsoleHelper.ExitWithMessage($"FATAL ERROR 0x{ex.HResult:X8}: {ex.Message}", exitCode:=ex.HResult, ConsoleColor.Red)

        End Try

        Dim exitCode As Integer = If(totalFailedFiles = 0, 0, 1)
        Dim exitColor As ConsoleColor = If(totalFailedFiles = 0, ConsoleColor.Green, ConsoleColor.Red)
        ConsoleHelper.ExitWithMessage($"All files have been processed. Modified: {totalUpdatedFiles:N0}; Skipped: {totalSkippedFiles:N0}; Failed: {totalFailedFiles:N0}.{If(isTestMode, " >>> TEST MODE IS ENABLED (NO FILES HAVE BEEN MODIFIED)", "")}", exitCode, exitColor)
    End Sub

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Prints command-line usage information to the console. 
    ''' <para></para>
    ''' Called whenever a mandatory or optional argument is missing or invalid.
    ''' </summary>
    <DebuggerStepThrough>
    Private Sub ShowUsage()

        Dim executableName As String = $"{Process.GetCurrentProcess().ProcessName}.exe"
        Dim validLineEndingStyles As String

#If NETCOREAPP Then
        validLineEndingStyles = String.Join(", ", [Enum].GetNames(Of LineEndingStyle)())
#Else
        validLineEndingStyles = String.Join(", ", [Enum].GetNames(GetType(LineEndingStyle)))
#End If

        ConsoleHelper.WriteColoredTextLine("Usage:", ConsoleColor.DarkCyan)
        Console.WriteLine($"  {executableName} <directory_path> <line_ending_style> [options]")
        Console.WriteLine()

        ConsoleHelper.WriteColoredTextLine("Mandatory Arguments:", ConsoleColor.DarkCyan)
        Console.WriteLine("  directory_path       The path to the root directory containing the *.cs or *.vb source-code files to process.")
        Console.WriteLine($"  line_ending_style    The target line ending formatting style to apply. Valid values are: {validLineEndingStyles}")
        Console.WriteLine()

        ConsoleHelper.WriteColoredTextLine("Options:", ConsoleColor.DarkCyan)
        Console.WriteLine("  -r, --recursive      Recursively process source-code files in all subdirectories.")
        Console.WriteLine("  -t, --test           Runs the application in Test Mode (dry-run), simulating the entire process without modifying actual files.")
        Console.WriteLine()

        ConsoleHelper.WriteColoredTextLine("Examples:", ConsoleColor.DarkCyan)
        Console.WriteLine($"  {executableName} ""C:\MySolution"" CRLF")
        Console.WriteLine($"  {executableName} ""C:\MySolution"" CR -r")
        Console.WriteLine($"  {executableName} ""C:\MySolution"" LF -r --test")
        Console.WriteLine()
    End Sub

#End Region

End Module
