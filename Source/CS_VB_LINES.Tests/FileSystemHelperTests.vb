#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.IO

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

Namespace CS_VB_LINES.Tests

    Public Class FileSystemHelperTests

        <Theory>
        <InlineData("C:\Test\Folder", "\\?\C:\Test\Folder")>
        <InlineData("\\Server\Share\File.txt", "\\?\UNC\Server\Share\File.txt")>
        <InlineData("Relative\Path.txt", "Relative\Path.txt")>
        <InlineData("\\?\C:\AlreadyExtended.txt", "\\?\C:\AlreadyExtended.txt")>
        Public Sub GetExtendedPath_ReturnsExpectedResult(input As String, expected As String)
            ' Act
            Dim result As String = FileSystemHelper.GetExtendedPath(input)

            ' Assert
            Assert.Equal(expected, result)
        End Sub

        <Theory>
        <InlineData("\\?\C:\Test\Folder", "C:\Test\Folder")>
        <InlineData("\\?\UNC\Server\Share", "\\Server\Share")>
        <InlineData("C:\Normal\Path.txt", "C:\Normal\Path.txt")>
        Public Sub GetNormalPath_ReturnsExpectedResult(input As String, expected As String)
            ' Act
            Dim result As String = FileSystemHelper.GetNormalPath(input)

            ' Assert
            Assert.Equal(expected, result)
        End Sub

        <Fact>
        Public Sub ReadSampleBytes_ThrowsArgumentOutOfRangeException_ForZeroMaxBytes()
            ' Arrange
            Dim fakePath As String = "C:\fake.txt"

            ' Act & Assert
            Dim ex As ArgumentOutOfRangeException = Assert.Throws(Of ArgumentOutOfRangeException)(
                Function() FileSystemHelper.ReadSampleBytes(fakePath, 0))

            Assert.Contains("maxBytes", ex.Message)
        End Sub

        <Fact>
        Public Sub ReadSampleBytes_WithThrowIfMaxBytesExceedsFileSize_ThrowsInvalidOperationException()
            ' Arrange
            Dim tempFilePath As String = Path.GetTempFileName()
            Try
                ' Write only 3 bytes to the file
                File.WriteAllText(tempFilePath, "123")

                ' Act & Assert: Request 10 bytes with strict length enforcement
                Dim ex As InvalidOperationException = Assert.Throws(Of InvalidOperationException)(
                    Function() FileSystemHelper.ReadSampleBytes(tempFilePath, maxBytes:=10, throwIfMaxBytesExceedsFileSize:=True))

                Assert.Contains("less than the requested maximum bytes", ex.Message)
            Finally
                If File.Exists(tempFilePath) Then
                    File.Delete(tempFilePath)
                End If
            End Try
        End Sub

    End Class

End Namespace
