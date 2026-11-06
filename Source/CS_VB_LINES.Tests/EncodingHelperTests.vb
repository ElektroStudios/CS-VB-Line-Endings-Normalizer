#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.IO
Imports System.Text

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

Namespace CS_VB_LINES.Tests

    Public Class EncodingHelperTests

        <Fact>
        Public Sub GetEncodingFromUtfEncodingKind_ReturnsCorrectEncoding()
            ' Act
            Dim encUtf8 As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.UTF8)
            Dim encUtf8Bom As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.UTF8_Bom)
            Dim encUnknown As Encoding = EncodingHelper.GetEncodingFromUtfEncodingKind(UtfEncodingKind.Unknown)

            ' Assert
            Assert.NotNull(encUtf8)
            Assert.Equal(0, encUtf8.GetPreamble().Length)

            Assert.NotNull(encUtf8Bom)
            Assert.True(encUtf8Bom.GetPreamble().Length > 0, "BOM encoding should have a preamble.")

            Assert.Null(encUnknown)
        End Sub

        <Fact>
        Public Sub TryGetUtfFileEncodingKind_WithUTF8BOM_DetectsSuccessfully()
            ' Arrange
            Dim tempFile As String = Path.GetTempFileName()
            Try
                ' UTF-8 with BOM
                File.WriteAllText(tempFile, "Hello World", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True))

                Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown

                ' Act
                Dim result As Boolean = EncodingHelper.TryGetUtfFileEncodingKind(tempFile, detectedKind)

                ' Assert
                Assert.True(result)
                Assert.Equal(UtfEncodingKind.UTF8_Bom, detectedKind)
            Finally
                If File.Exists(tempFile) Then
                    File.Delete(tempFile)
                End If
            End Try
        End Sub

        <Fact>
        Public Sub TryGetUtfFileEncodingKind_EmptyFile_ReturnsFalseAndUnknown()
            ' Arrange
            Dim tempFile As String = Path.GetTempFileName()
            Try
                File.WriteAllBytes(tempFile, Array.Empty(Of Byte)())

                Dim detectedKind As UtfEncodingKind = UtfEncodingKind.UTF8 ' Initialize to something else

                ' Act
                Dim result As Boolean = EncodingHelper.TryGetUtfFileEncodingKind(tempFile, detectedKind)

                ' Assert
                Assert.False(result)
                Assert.Equal(UtfEncodingKind.Unknown, detectedKind)
            Finally
                If File.Exists(tempFile) Then
                    File.Delete(tempFile)
                End If
            End Try
        End Sub

    End Class

End Namespace
