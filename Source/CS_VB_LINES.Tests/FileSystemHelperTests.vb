#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.IO
Imports System.Linq
Imports System.Text

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

#Region " FileSystemHelperTests "

''' <summary>
''' Tests for <see cref="FileSystemHelper"/>: enumeration of source files, reading of file samples 
''' and conversion of paths to and from the extended-length format.
''' </summary>
Public Class FileSystemHelperTests

#Region " Private Methods "

    ''' <summary>
    ''' Enumerates the source files of a directory and returns their paths relative to it (using '/'), sorted.
    ''' </summary>
    Private Shared Function EnumerateRelative(rootPath As String, searchOption As SearchOption) As String()

        Dim result As New List(Of String)()

        For Each filePath As String In FileSystemHelper.EnumerateSourceFiles(rootPath, searchOption)
            result.Add(Path.GetRelativePath(rootPath, filePath).Replace("\"c, "/"c))
        Next filePath

        result.Sort(StringComparer.Ordinal)

        Return result.ToArray()

    End Function

#End Region

#Region " GetExtendedPath "

    ''' <summary>
    ''' Conversions that only make sense with Windows paths (drive letters and UNC shares).
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("C:\Test\Folder", "\\?\C:\Test\Folder")>
    <InlineData("\\Server\Share\File.txt", "\\?\UNC\Server\Share\File.txt")>
    <InlineData("D:\", "\\?\D:\")>
    <InlineData("C:\Folder With Spaces\File (1).txt", "\\?\C:\Folder With Spaces\File (1).txt")>
    Public Sub GetExtendedPath_WindowsRootedPaths_GetThePrefix(input As String, expected As String)

        Assert.Equal(expected, FileSystemHelper.GetExtendedPath(input))

    End Sub

    ''' <summary>
    ''' Relative paths cannot be converted, and already extended paths must not get the prefix twice.
    ''' </summary>
    <Theory>
    <InlineData("Relative\Path.txt", "Relative\Path.txt")>
    <InlineData("File.txt", "File.txt")>
    <InlineData("..\Sibling\File.txt", "..\Sibling\File.txt")>
    <InlineData("\\?\C:\AlreadyExtended.txt", "\\?\C:\AlreadyExtended.txt")>
    <InlineData("\\?\UNC\Server\Share\AlreadyExtended.txt", "\\?\UNC\Server\Share\AlreadyExtended.txt")>
    Public Sub GetExtendedPath_RelativeOrAlreadyExtendedPaths_AreReturnedUnchanged(input As String, expected As String)

        Assert.Equal(expected, FileSystemHelper.GetExtendedPath(input))

    End Sub

    <Fact>
    Public Sub GetExtendedPath_NullEmptyOrBlank_AreReturnedUnchanged()

        Assert.Null(FileSystemHelper.GetExtendedPath(Nothing))
        Assert.Equal(String.Empty, FileSystemHelper.GetExtendedPath(String.Empty))
        Assert.Equal("   ", FileSystemHelper.GetExtendedPath("   "))

    End Sub

    ''' <summary>
    ''' Applying the conversion twice must be the same as applying it once.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("C:\Test\Folder")>
    <InlineData("\\Server\Share\File.txt")>
    Public Sub GetExtendedPath_IsIdempotent(input As String)

        Dim once As String = FileSystemHelper.GetExtendedPath(input)
        Dim twice As String = FileSystemHelper.GetExtendedPath(once)

        Assert.Equal(once, twice)

    End Sub

#End Region

#Region " GetNormalPath "

    <Theory>
    <InlineData("\\?\C:\Test\Folder", "C:\Test\Folder")>
    <InlineData("\\?\UNC\Server\Share", "\\Server\Share")>
    <InlineData("\\?\unc\Server\Share\File.txt", "\\Server\Share\File.txt")>
    <InlineData("C:\Normal\Path.txt", "C:\Normal\Path.txt")>
    <InlineData("\\Server\Share\Normal.txt", "\\Server\Share\Normal.txt")>
    <InlineData("Relative\Path.txt", "Relative\Path.txt")>
    Public Sub GetNormalPath_ReturnsExpectedResult(input As String, expected As String)

        Assert.Equal(expected, FileSystemHelper.GetNormalPath(input))

    End Sub

    <Fact>
    Public Sub GetNormalPath_NullEmptyOrBlank_AreReturnedUnchanged()

        Assert.Null(FileSystemHelper.GetNormalPath(Nothing))
        Assert.Equal(String.Empty, FileSystemHelper.GetNormalPath(String.Empty))
        Assert.Equal("   ", FileSystemHelper.GetNormalPath("   "))

    End Sub

    ''' <summary>
    ''' Converting to the extended format and back must give the original path.
    ''' </summary>
    <WindowsOnlyTheory>
    <InlineData("C:\Test\Folder\File.cs")>
    <InlineData("\\Server\Share\Folder\File.vb")>
    <InlineData("D:\")>
    Public Sub GetNormalPath_UndoesGetExtendedPath(original As String)

        Assert.Equal(original, FileSystemHelper.GetNormalPath(FileSystemHelper.GetExtendedPath(original)))

    End Sub

#End Region

#Region " ReadSampleBytes "

    <Fact>
    Public Sub ReadSampleBytes_ThrowsArgumentOutOfRangeException_ForZeroMaxBytes()

        Dim fakePath As String = "C:\fake.txt"

        Dim ex As ArgumentOutOfRangeException = Assert.Throws(Of ArgumentOutOfRangeException)(
            Function() FileSystemHelper.ReadSampleBytes(fakePath, 0))

        Assert.Contains("maxBytes", ex.Message)

    End Sub

    <Theory>
    <InlineData(-1)>
    <InlineData(Integer.MinValue)>
    Public Sub ReadSampleBytes_NegativeMaxBytes_ThrowsArgumentOutOfRangeException(maxBytes As Integer)

        Dim ex As ArgumentOutOfRangeException = Assert.Throws(Of ArgumentOutOfRangeException)(
            Function() FileSystemHelper.ReadSampleBytes("any.txt", maxBytes))

        Assert.Equal("maxBytes", ex.ParamName)

    End Sub

    ''' <summary>
    ''' The path is validated before the file is touched. The exact exception type differs between the .NET Framework 4.8 build
    ''' (always <see cref="ArgumentNullException"/>) and the .NET 10 build (<see cref="ArgumentNullException"/> only for Nothing, 
    ''' <see cref="ArgumentException"/> for empty and blank texts), so the common base type is asserted.
    ''' </summary>
    <Theory>
    <InlineData(Nothing)>
    <InlineData("")>
    <InlineData("   ")>
    Public Sub ReadSampleBytes_NullEmptyOrBlankPath_ThrowsArgumentException(filePath As String)

        Assert.ThrowsAny(Of ArgumentException)(
            Function() FileSystemHelper.ReadSampleBytes(filePath, 10))

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_ReturnsAllBytes_WhenTheFileIsSmallerThanMaxBytes()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("small.bin", New Byte() {1, 2, 3})

            Dim result As Byte() = FileSystemHelper.ReadSampleBytes(filePath, 100)

            Assert.Equal(New Byte() {1, 2, 3}, result)

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_ReturnsOnlyTheFirstMaxBytes_WhenTheFileIsBigger()

        Using temp As New TempDirectory()

            Dim content(9999) As Byte
            For index As Integer = 0 To content.Length - 1
                content(index) = CByte(index Mod 251)
            Next index
            Dim filePath As String = temp.WriteBytes("big.bin", content)

            Dim result As Byte() = FileSystemHelper.ReadSampleBytes(filePath, 4096)

            Assert.Equal(4096, result.Length)
            Assert.Equal(content.Take(4096).ToArray(), result)

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_FileWithExactlyMaxBytes_ReturnsAllBytes_EvenWhenStrict()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("exact.bin", New Byte() {9, 8, 7, 6})

            Assert.Equal(New Byte() {9, 8, 7, 6}, FileSystemHelper.ReadSampleBytes(filePath, 4))
            Assert.Equal(New Byte() {9, 8, 7, 6}, FileSystemHelper.ReadSampleBytes(filePath, 4, throwIfMaxBytesExceedsFileSize:=True))

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_BiggerFile_DoesNotThrowWhenStrict()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("big.bin", New Byte(99) {})

            Assert.Equal(10, FileSystemHelper.ReadSampleBytes(filePath, 10, throwIfMaxBytesExceedsFileSize:=True).Length)

        End Using

    End Sub

    ''' <summary>
    ''' An empty file gives an empty array (and not an exception) in the default mode, 
    ''' and an <see cref="InvalidOperationException"/> in strict mode.
    ''' </summary>
    <Fact>
    Public Sub ReadSampleBytes_EmptyFile_BehavesAccordingToTheStrictFlag()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("empty.bin", Array.Empty(Of Byte)())

            Assert.Empty(FileSystemHelper.ReadSampleBytes(filePath, 10))
            Assert.Throws(Of InvalidOperationException)(
                Function() FileSystemHelper.ReadSampleBytes(filePath, 10, throwIfMaxBytesExceedsFileSize:=True))

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_WithThrowIfMaxBytesExceedsFileSize_ThrowsInvalidOperationException()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteText("small.txt", "123", New UTF8Encoding(False))

            Dim ex As InvalidOperationException = Assert.Throws(Of InvalidOperationException)(
                Function() FileSystemHelper.ReadSampleBytes(filePath, maxBytes:=10, throwIfMaxBytesExceedsFileSize:=True))

            Assert.Contains("less than the requested maximum bytes", ex.Message)

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_MissingFile_ThrowsFileNotFoundException()

        Using temp As New TempDirectory()

            Dim missingPath As String = temp.GetPath("missing.bin")

            Assert.Throws(Of FileNotFoundException)(
                Function() FileSystemHelper.ReadSampleBytes(missingPath, 10))

        End Using

    End Sub

    <Fact>
    Public Sub ReadSampleBytes_MissingDirectory_ThrowsDirectoryNotFoundException()

        Using temp As New TempDirectory()

            Dim missingPath As String = temp.GetPath("missing-folder/file.bin")

            Assert.Throws(Of DirectoryNotFoundException)(
                Function() FileSystemHelper.ReadSampleBytes(missingPath, 10))

        End Using

    End Sub

    ''' <summary>
    ''' The bytes are returned untouched: no text decoding, no BOM removal, no line-ending conversion.
    ''' </summary>
    <Fact>
    Public Sub ReadSampleBytes_ReturnsTheRawBytes_IncludingBomAndLineBreaks()

        Using temp As New TempDirectory()

            Dim raw As Byte() = {&HEF, &HBB, &HBF, &HD, &HA, 0, &HFF}
            Dim filePath As String = temp.WriteBytes("raw.bin", raw)

            Assert.Equal(raw, FileSystemHelper.ReadSampleBytes(filePath, 1000))

        End Using

    End Sub

    ''' <summary>
    ''' Reading a sample never changes the file.
    ''' </summary>
    <Fact>
    Public Sub ReadSampleBytes_DoesNotModifyTheFile()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("keep.bin", New Byte() {1, 2, 3, 4, 5})
            Dim stampBefore As DateTime = File.GetLastWriteTimeUtc(filePath)

            FileSystemHelper.ReadSampleBytes(filePath, 3)

            Assert.Equal(New Byte() {1, 2, 3, 4, 5}, File.ReadAllBytes(filePath))
            Assert.Equal(stampBefore, File.GetLastWriteTimeUtc(filePath))

        End Using

    End Sub

    ''' <summary>
    ''' The file handle is released after the call, so the file can be deleted right away.
    ''' </summary>
    <Fact>
    Public Sub ReadSampleBytes_ReleasesTheFileHandle()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteBytes("handle.bin", New Byte() {1, 2, 3})

            FileSystemHelper.ReadSampleBytes(filePath, 3)
            File.Delete(filePath)

            Assert.False(File.Exists(filePath))

        End Using

    End Sub

#End Region

#Region " EnumerateSourceFiles "

    ''' <summary>
    ''' Creates a typical tree:
    ''' <code>
    ''' root: a.cs, b.vb, notes.txt
    ''' root/sub: c.cs, d.vb
    ''' root/sub/deep: e.vb
    ''' </code>
    ''' </summary>
    Private Shared Sub CreateSampleTree(temp As TempDirectory)

        temp.WriteText("a.cs", "x", Encoding.UTF8)
        temp.WriteText("b.vb", "x", Encoding.UTF8)
        temp.WriteText("notes.txt", "x", Encoding.UTF8)
        temp.WriteText("sub/c.cs", "x", Encoding.UTF8)
        temp.WriteText("sub/d.vb", "x", Encoding.UTF8)
        temp.WriteText("sub/deep/e.vb", "x", Encoding.UTF8)

    End Sub

    <Fact>
    Public Sub EnumerateSourceFiles_TopDirectoryOnly_ReturnsOnlyTheFilesOfTheRoot()

        Using temp As New TempDirectory()

            FileSystemHelperTests.CreateSampleTree(temp)

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.TopDirectoryOnly)

            Assert.Equal(New String() {"a.cs", "b.vb"}, result)

        End Using

    End Sub

    <Fact>
    Public Sub EnumerateSourceFiles_AllDirectories_ReturnsTheFilesOfEverySubdirectory()

        Using temp As New TempDirectory()

            FileSystemHelperTests.CreateSampleTree(temp)

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.AllDirectories)

            Assert.Equal(New String() {"a.cs", "b.vb", "sub/c.cs", "sub/d.vb", "sub/deep/e.vb"}, result)

        End Using

    End Sub

    ''' <summary>
    ''' Only ".cs" and ".vb" files are source files. Similar extensions (project files, Razor pages, scripts...) are not.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_IgnoresOtherExtensionsEvenIfTheyStartLikeASourceExtension()

        Using temp As New TempDirectory()

            For Each fileName As String In New String() {"a.cs", "b.vb", "c.csproj", "d.vbproj", "e.cshtml", "f.csx", "g.vbs", "h.cs.bak", "i.vb.tmp", "j.txt", "k", "l.designer", "m.c", "n.v"}
                temp.WriteText(fileName, "x", Encoding.UTF8)
            Next fileName

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.TopDirectoryOnly)

            Assert.Equal(New String() {"a.cs", "b.vb"}, result)

        End Using

    End Sub

    ''' <summary>
    ''' A directory whose name looks like a source file is not a file, and must not be returned.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_DirectoryNamedLikeASourceFile_IsNotReturned()

        Using temp As New TempDirectory()

            temp.CreateSubdirectory("folder.cs")
            temp.CreateSubdirectory("folder.vb")
            temp.WriteText("folder.cs/real.cs", "x", Encoding.UTF8)

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.AllDirectories)

            Assert.Equal(New String() {"folder.cs/real.cs"}, result)

        End Using

    End Sub

    <Fact>
    Public Sub EnumerateSourceFiles_EmptyDirectory_ReturnsNothing()

        Using temp As New TempDirectory()

            Assert.Empty(FileSystemHelper.EnumerateSourceFiles(temp.FullPath, SearchOption.AllDirectories).ToList())

        End Using

    End Sub

    ''' <summary>
    ''' Empty subdirectories and subdirectories without source files do not break the traversal.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_EmptyAndIrrelevantSubdirectories_AreTraversedWithoutProblems()

        Using temp As New TempDirectory()

            temp.CreateSubdirectory("empty")
            temp.WriteText("docs/readme.md", "x", Encoding.UTF8)
            temp.WriteText("last/z.cs", "x", Encoding.UTF8)

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.AllDirectories)

            Assert.Equal(New String() {"last/z.cs"}, result)

        End Using

    End Sub

    ''' <summary>
    ''' The traversal is iterative (explicit stack), so a deep directory tree is not a problem.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_DeepDirectoryTree_IsFullyTraversed()

        Using temp As New TempDirectory()

            Dim relativePath As String = String.Empty
            For level As Integer = 1 To 40
                relativePath = $"{relativePath}d/"
            Next level
            temp.WriteText($"{relativePath}deep.vb", "x", Encoding.UTF8)

            Dim result As String() = FileSystemHelperTests.EnumerateRelative(temp.FullPath, SearchOption.AllDirectories)

            Assert.Equal(New String() {$"{relativePath}deep.vb"}, result)

        End Using

    End Sub

    <Fact>
    Public Sub EnumerateSourceFiles_ReturnsFullPathsInsideTheRoot()

        Using temp As New TempDirectory()

            temp.WriteText("sub/a.cs", "x", Encoding.UTF8)

            Dim result As String = FileSystemHelper.EnumerateSourceFiles(temp.FullPath, SearchOption.AllDirectories).Single()

            Assert.True(Path.IsPathRooted(result))
            Assert.Equal(temp.GetPath(Path.Combine("sub", "a.cs")), result)

        End Using

    End Sub

    ''' <summary>
    ''' The method is a lazy iterator: the root is validated and read when the sequence is enumerated, not when the method is called.
    ''' Files created after the call but before the enumeration are therefore returned.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_IsLazilyEvaluated()

        Using temp As New TempDirectory()

            Dim sequence As IEnumerable(Of String) = FileSystemHelper.EnumerateSourceFiles(temp.FullPath, SearchOption.TopDirectoryOnly)

            temp.WriteText("late.cs", "x", Encoding.UTF8)

            Assert.Equal(New String() {temp.GetPath("late.cs")}, sequence.ToArray())

        End Using

    End Sub

    ''' <summary>
    ''' Both the CS and the VB files of the same directory are returned.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_ReturnsEachFileOnlyOnce()

        Using temp As New TempDirectory()

            temp.WriteText("same.cs", "x", Encoding.UTF8)
            temp.WriteText("same.vb", "x", Encoding.UTF8)

            Dim result As List(Of String) = FileSystemHelper.EnumerateSourceFiles(temp.FullPath, SearchOption.AllDirectories).ToList()

            Assert.Equal(2, result.Count)
            Assert.Equal(2, result.Distinct(StringComparer.Ordinal).Count())

        End Using

    End Sub

    <Fact>
    Public Sub EnumerateSourceFiles_MissingRoot_ThrowsDirectoryNotFoundExceptionWhenEnumerated()

        Using temp As New TempDirectory()

            Dim missingRoot As String = temp.GetPath("missing")

            Assert.Throws(Of DirectoryNotFoundException)(
                Function() FileSystemHelper.EnumerateSourceFiles(missingRoot, SearchOption.AllDirectories).ToList())

        End Using

    End Sub

    ''' <summary>
    ''' A path of a FILE is not a valid root directory.
    ''' </summary>
    <Fact>
    Public Sub EnumerateSourceFiles_RootThatIsAFile_ThrowsDirectoryNotFoundException()

        Using temp As New TempDirectory()

            Dim filePath As String = temp.WriteText("a.cs", "x", Encoding.UTF8)

            Assert.Throws(Of DirectoryNotFoundException)(
                Function() FileSystemHelper.EnumerateSourceFiles(filePath, SearchOption.TopDirectoryOnly).ToList())

        End Using

    End Sub

    <Theory>
    <InlineData(Nothing)>
    <InlineData("")>
    <InlineData("   ")>
    Public Sub EnumerateSourceFiles_NullEmptyOrBlankRoot_ThrowsArgumentException(rootPath As String)

        Assert.ThrowsAny(Of ArgumentException)(
            Function() FileSystemHelper.EnumerateSourceFiles(rootPath, SearchOption.AllDirectories).ToList())

    End Sub

    <Theory>
    <InlineData(-1)>
    <InlineData(2)>
    <InlineData(99)>
    Public Sub EnumerateSourceFiles_UndefinedSearchOption_ThrowsInvalidEnumArgumentException(value As Integer)

        Using temp As New TempDirectory()

            Dim rootPath As String = temp.FullPath

            Dim ex As InvalidEnumArgumentException = Assert.Throws(Of InvalidEnumArgumentException)(
                Function() FileSystemHelper.EnumerateSourceFiles(rootPath, CType(value, SearchOption)).ToList())

            Assert.Equal("searchOption", ex.ParamName)

        End Using

    End Sub

#End Region

End Class

#End Region
