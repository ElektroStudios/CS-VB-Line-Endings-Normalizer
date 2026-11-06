#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

Namespace CS_VB_LINES.Tests

    Public Class StringsHelperTests

        <Fact>
        Public Sub NormalizeLineEndings_To_CRLF_ConvertsCorrectly()
            ' Arrange
            Dim input As String = $"Line1{Constants.vbLf}Line2{Constants.vbCr}Line3{Constants.vbCrLf}"
            Dim expected As String = $"Line1{Constants.vbCrLf}Line2{Constants.vbCrLf}Line3{Constants.vbCrLf}"

            ' Act
            Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF)

            ' Assert
            Assert.Equal(expected, result)
        End Sub

        <Fact>
        Public Sub NormalizeLineEndings_To_LF_ConvertsCorrectly()
            ' Arrange
            Dim input As String = $"Line1{Constants.vbLf}Line2{Constants.vbCr}Line3{Constants.vbCrLf}"
            Dim expected As String = $"Line1{Constants.vbLf}Line2{Constants.vbLf}Line3{Constants.vbLf}"

            ' Act
            Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.LF)

            ' Assert
            Assert.Equal(expected, result)
        End Sub

        <Fact>
        Public Sub NormalizeLineEndings_To_CR_ConvertsCorrectly()
            ' Arrange
            Dim input As String = $"Line1{Constants.vbLf}Line2{Constants.vbCr}Line3{Constants.vbCrLf}"
            Dim expected As String = $"Line1{Constants.vbCr}Line2{Constants.vbCr}Line3{Constants.vbCr}"

            ' Act
            Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CR)

            ' Assert
            Assert.Equal(expected, result)
        End Sub

        <Fact>
        Public Sub NormalizeLineEndings_WithInvalidTarget_ThrowsException()
            ' Arrange
            Dim input As String = "Test"
            Dim invalidTarget As LineEndingStyle = CType(999, LineEndingStyle)

            ' Act & Assert
            Assert.Throws(Of ComponentModel.InvalidEnumArgumentException)(
                Function() StringsHelper.NormalizeLineEndings(input, invalidTarget))
        End Sub

        <Fact>
        Public Sub NormalizeLineEndings_EmptyOrNoNewlines_ReturnsOriginalString()
            ' Arrange
            Dim emptyInput As String = String.Empty
            Dim singleLineInput As String = "public class Target { }"

            ' Act
            Dim emptyResult As String = StringsHelper.NormalizeLineEndings(emptyInput, LineEndingStyle.CRLF)
            Dim singleLineResult As String = StringsHelper.NormalizeLineEndings(singleLineInput, LineEndingStyle.LF)

            ' Assert
            Assert.Equal(emptyInput, emptyResult)
            Assert.Equal(singleLineInput, singleLineResult)
        End Sub

    End Class

End Namespace
