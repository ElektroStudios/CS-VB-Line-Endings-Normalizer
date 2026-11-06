#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.ComponentModel
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions

Imports CS_VB_Line_Endings_Normalizer

Imports Xunit

#End Region

#Region " StringsHelperTests "

''' <summary>
''' Tests for <see cref="StringsHelper.NormalizeLineEndings(String, LineEndingStyle)"/>.
''' </summary>
Public Class StringsHelperTests

#Region " Private Methods "

    ''' <summary>
    ''' Returns the line-ending sequence that corresponds to a style.
    ''' </summary>
    Private Shared Function GetSequence(style As LineEndingStyle) As String

        Select Case style
            Case LineEndingStyle.CRLF
                Return vbCrLf
            Case LineEndingStyle.LF
                Return vbLf
            Case LineEndingStyle.CR
                Return vbCr
            Case Else
                Throw New InvalidEnumArgumentException(NameOf(style), style, GetType(LineEndingStyle))
        End Select

    End Function

    ''' <summary>
    ''' Independent reference implementation: splits the text on every line break (CRLF, LF or CR, where CRLF counts once) 
    ''' and joins the lines again with the sequence of the target style.
    ''' </summary>
    Private Shared Function ReferenceNormalize(text As String, style As LineEndingStyle) As String

        Dim lines As String() = Regex.Split(text, "\r\n|\r|\n")

        Return String.Join(StringsHelperTests.GetSequence(style), lines)

    End Function

    ''' <summary>
    ''' Counts the line breaks (CRLF, LF or CR, where CRLF counts once) of a text.
    ''' </summary>
    Private Shared Function CountLineBreaks(text As String) As Integer

        Return Regex.Matches(text, "\r\n|\r|\n").Count

    End Function

#End Region

#Region " Original Cases "

    <Fact>
    Public Sub NormalizeLineEndings_To_CRLF_ConvertsCorrectly()

        Dim input As String = $"Line1{vbLf}Line2{vbCr}Line3{vbCrLf}"
        Dim expected As String = $"Line1{vbCrLf}Line2{vbCrLf}Line3{vbCrLf}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF)

        Assert.Equal(expected, result)

    End Sub

    <Fact>
    Public Sub NormalizeLineEndings_To_LF_ConvertsCorrectly()

        Dim input As String = $"Line1{vbLf}Line2{vbCr}Line3{vbCrLf}"
        Dim expected As String = $"Line1{vbLf}Line2{vbLf}Line3{vbLf}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.LF)

        Assert.Equal(expected, result)

    End Sub

    <Fact>
    Public Sub NormalizeLineEndings_To_CR_ConvertsCorrectly()

        Dim input As String = $"Line1{vbLf}Line2{vbCr}Line3{vbCrLf}"
        Dim expected As String = $"Line1{vbCr}Line2{vbCr}Line3{vbCr}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CR)

        Assert.Equal(expected, result)

    End Sub

    <Fact>
    Public Sub NormalizeLineEndings_WithInvalidTarget_ThrowsException()

        Dim invalidTarget As LineEndingStyle = CType(999, LineEndingStyle)

        Assert.Throws(Of InvalidEnumArgumentException)(
            Function() StringsHelper.NormalizeLineEndings("Test", invalidTarget))

    End Sub

    <Fact>
    Public Sub NormalizeLineEndings_EmptyOrNoNewlines_ReturnsOriginalString()

        Dim emptyInput As String = String.Empty
        Dim singleLineInput As String = "public class Target { }"

        Dim emptyResult As String = StringsHelper.NormalizeLineEndings(emptyInput, LineEndingStyle.CRLF)
        Dim singleLineResult As String = StringsHelper.NormalizeLineEndings(singleLineInput, LineEndingStyle.LF)

        Assert.Equal(emptyInput, emptyResult)
        Assert.Equal(singleLineInput, singleLineResult)

    End Sub

#End Region

#Region " Individual Line Breaks "

    ''' <summary>
    ''' Every kind of line break, alone, converted to every style.
    ''' </summary>
    <Theory>
    <InlineData("LF", "CRLF", vbCrLf)>
    <InlineData("LF", "LF", vbLf)>
    <InlineData("LF", "CR", vbCr)>
    <InlineData("CR", "CRLF", vbCrLf)>
    <InlineData("CR", "LF", vbLf)>
    <InlineData("CR", "CR", vbCr)>
    <InlineData("CRLF", "CRLF", vbCrLf)>
    <InlineData("CRLF", "LF", vbLf)>
    <InlineData("CRLF", "CR", vbCr)>
    Public Sub NormalizeLineEndings_SingleBreak_IsConvertedToTheTargetSequence(sourceName As String,
                                                                               targetName As String,
                                                                               expectedBreak As String)

        Dim source As String = StringsHelperTests.GetSequence(CType([Enum].Parse(GetType(LineEndingStyle), sourceName), LineEndingStyle))
        Dim target As LineEndingStyle = CType([Enum].Parse(GetType(LineEndingStyle), targetName), LineEndingStyle)

        Dim result As String = StringsHelper.NormalizeLineEndings($"a{source}b", target)

        Assert.Equal($"a{expectedBreak}b", result)

    End Sub

    ''' <summary>
    ''' A CRLF pair must be converted as ONE line break, never as two (a classic bug of chained Replace calls).
    ''' </summary>
    <Theory>
    <InlineData(LineEndingStyle.CRLF)>
    <InlineData(LineEndingStyle.LF)>
    <InlineData(LineEndingStyle.CR)>
    Public Sub NormalizeLineEndings_CrLfPair_IsNotConvertedTwice(style As LineEndingStyle)

        Dim result As String = StringsHelper.NormalizeLineEndings($"a{vbCrLf}b", style)

        Assert.Equal(1, StringsHelperTests.CountLineBreaks(result))

    End Sub

    ''' <summary>
    ''' LF followed by CR (reversed pair) are two different line breaks, not one.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_LfFollowedByCr_AreTwoLineBreaks()

        Dim result As String = StringsHelper.NormalizeLineEndings($"a{vbLf}{vbCr}b", LineEndingStyle.LF)

        Assert.Equal($"a{vbLf}{vbLf}b", result)

    End Sub

    ''' <summary>
    ''' CR CR LF is a lone CR followed by a CRLF pair: two line breaks.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_CrCrLf_AreTwoLineBreaks()

        Dim result As String = StringsHelper.NormalizeLineEndings($"a{vbCr}{vbCr}{vbLf}b", LineEndingStyle.CRLF)

        Assert.Equal($"a{vbCrLf}{vbCrLf}b", result)

    End Sub

    ''' <summary>
    ''' A text that is only line breaks keeps the same number of line breaks.
    ''' </summary>
    <Theory>
    <InlineData(LineEndingStyle.CRLF)>
    <InlineData(LineEndingStyle.LF)>
    <InlineData(LineEndingStyle.CR)>
    Public Sub NormalizeLineEndings_OnlyLineBreaks_KeepsTheirCount(style As LineEndingStyle)

        Dim input As String = $"{vbLf}{vbCr}{vbCrLf}{vbLf}{vbLf}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, style)

        Assert.Equal(StringsHelperTests.CountLineBreaks(input), StringsHelperTests.CountLineBreaks(result))
        Assert.Equal(StringsHelperTests.GetSequence(style).Length * 5, result.Length)

    End Sub

#End Region

#Region " Content Preservation "

    ''' <summary>
    ''' A text without a trailing line break must not get one, and a trailing one must be kept.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_TrailingLineBreak_IsPreservedOrAbsentAsInTheOriginal()

        Assert.Equal($"a{vbCrLf}b", StringsHelper.NormalizeLineEndings($"a{vbLf}b", LineEndingStyle.CRLF))
        Assert.Equal($"a{vbCrLf}b{vbCrLf}", StringsHelper.NormalizeLineEndings($"a{vbLf}b{vbLf}", LineEndingStyle.CRLF))

    End Sub

    ''' <summary>
    ''' Leading, trailing and consecutive blank lines are kept (one output break per input break).
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_BlankLines_AreKept()

        Dim input As String = $"{vbLf}{vbLf}a{vbLf}{vbLf}{vbLf}b{vbLf}{vbLf}"
        Dim expected As String = $"{vbCrLf}{vbCrLf}a{vbCrLf}{vbCrLf}{vbCrLf}b{vbCrLf}{vbCrLf}"

        Assert.Equal(expected, StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF))

    End Sub

    ''' <summary>
    ''' Only line breaks are touched: tabs, spaces (including trailing ones), form feeds, 
    ''' and the Unicode line separators (NEL, LS, PS) must stay exactly as they are.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_OtherWhitespaceAndUnicodeSeparators_AreNotTouched()

        Dim exotic As String = $"{vbTab}  x  {ChrW(&HC)}{ChrW(&H85)}{ChrW(&H2028)}{ChrW(&H2029)}{ChrW(&HA0)}{ChrW(&HB)}"
        Dim input As String = $"{exotic}{vbLf}{exotic}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF)

        Assert.Equal($"{exotic}{vbCrLf}{exotic}", result)

    End Sub

    ''' <summary>
    ''' Non-ASCII text, surrogate pairs and a few typical code constructs survive the normalization.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_UnicodeText_IsPreserved()

        Dim line As String = $"// ñandú — 日本語 — {ChrW(&HD83D)}{ChrW(&HDE00)} — ""quoted"" 'x' \n \r"
        Dim input As String = $"{line}{vbLf}{line}{vbCr}{line}"

        Dim result As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.LF)

        Assert.Equal($"{line}{vbLf}{line}{vbLf}{line}", result)

    End Sub

    ''' <summary>
    ''' The escape sequences written as text (backslash + 'r', backslash + 'n') are source code, not line breaks.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_LiteralBackslashEscapes_AreNotLineBreaks()

        Dim input As String = "Console.Write(""a\r\nb"")"

        Assert.Equal(input, StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF))

    End Sub

#End Region

#Region " Invariants "

    ''' <summary>
    ''' Normalizing twice gives the same result as normalizing once.
    ''' </summary>
    <Theory>
    <InlineData(LineEndingStyle.CRLF)>
    <InlineData(LineEndingStyle.LF)>
    <InlineData(LineEndingStyle.CR)>
    Public Sub NormalizeLineEndings_IsIdempotent(style As LineEndingStyle)

        Dim input As String = $"a{vbLf}b{vbCr}c{vbCrLf}d{vbCr}{vbLf}{vbLf}{vbCr}"

        Dim once As String = StringsHelper.NormalizeLineEndings(input, style)
        Dim twice As String = StringsHelper.NormalizeLineEndings(once, style)

        Assert.Equal(once, twice)

    End Sub

    ''' <summary>
    ''' The result of a style never contains the characters of the other styles.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_Result_ContainsOnlyTheTargetSequence()

        Dim input As String = $"a{vbLf}b{vbCr}c{vbCrLf}d"

        Dim lf As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.LF)
        Dim cr As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CR)
        Dim crlf As String = StringsHelper.NormalizeLineEndings(input, LineEndingStyle.CRLF)

        Assert.DoesNotContain(vbCr, lf)
        Assert.DoesNotContain(vbLf, cr)
        Assert.Equal(3, crlf.Count(Function(c As Char) c = ControlChars.Lf))
        Assert.Equal(3, crlf.Count(Function(c As Char) c = ControlChars.Cr))
        Assert.Equal(3, StringsHelperTests.CountLineBreaks(crlf))

    End Sub

    ''' <summary>
    ''' Converting between styles never loses or invents a line break: 
    ''' CRLF → LF → CR → CRLF gives back the original CRLF text.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_RoundTripBetweenStyles_IsLossless()

        Dim original As String = $"first{vbCrLf}{vbCrLf}third{vbCrLf}last"

        Dim lf As String = StringsHelper.NormalizeLineEndings(original, LineEndingStyle.LF)
        Dim cr As String = StringsHelper.NormalizeLineEndings(lf, LineEndingStyle.CR)
        Dim crlf As String = StringsHelper.NormalizeLineEndings(cr, LineEndingStyle.CRLF)

        Assert.Equal(original, crlf)

    End Sub

    ''' <summary>
    ''' Randomized comparison against an independent reference implementation. 
    ''' The seed is fixed, so the test is fully deterministic and any failure can be reproduced.
    ''' </summary>
    <Theory>
    <InlineData(LineEndingStyle.CRLF)>
    <InlineData(LineEndingStyle.LF)>
    <InlineData(LineEndingStyle.CR)>
    Public Sub NormalizeLineEndings_RandomTexts_MatchTheReferenceImplementation(style As LineEndingStyle)

        Dim random As New Random(20260611)
        Dim alphabet As Char() = {"a"c, "b"c, " "c, ControlChars.Cr, ControlChars.Lf, ControlChars.Cr, ControlChars.Lf}

        For iteration As Integer = 0 To 999

            Dim length As Integer = random.Next(0, 40)
            Dim builder As New StringBuilder(length)
            For position As Integer = 0 To length - 1
                builder.Append(alphabet(random.Next(0, alphabet.Length)))
            Next position

            Dim input As String = builder.ToString()
            Dim expected As String = StringsHelperTests.ReferenceNormalize(input, style)
            Dim actual As String = StringsHelper.NormalizeLineEndings(input, style)

            Assert.True(expected = actual,
                        $"Mismatch for style {style} with input (escaped): {input.Replace(vbCr, "\r").Replace(vbLf, "\n")}")

        Next iteration

    End Sub

    ''' <summary>
    ''' A big text (100,000 lines) is normalized correctly.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_BigText_IsNormalizedCompletely()

        Dim builder As New StringBuilder()
        For lineNumber As Integer = 0 To 99999
            builder.Append("Line ").Append(lineNumber).Append(vbLf)
        Next lineNumber

        Dim result As String = StringsHelper.NormalizeLineEndings(builder.ToString(), LineEndingStyle.CRLF)

        Assert.Equal(100000, StringsHelperTests.CountLineBreaks(result))
        Assert.EndsWith($"Line 99999{vbCrLf}", result)
        Assert.DoesNotContain($"{vbLf}{vbLf}", result)

    End Sub

#End Region

#Region " Invalid Arguments "

    <Theory>
    <InlineData(-1)>
    <InlineData(3)>
    <InlineData(999)>
    <InlineData(Integer.MaxValue)>
    Public Sub NormalizeLineEndings_UndefinedStyle_ThrowsInvalidEnumArgumentExceptionNamingTheParameter(value As Integer)

        Dim ex As InvalidEnumArgumentException = Assert.Throws(Of InvalidEnumArgumentException)(
            Function() StringsHelper.NormalizeLineEndings("text", CType(value, LineEndingStyle)))

        Assert.Equal("targetStyle", ex.ParamName)

    End Sub

    ''' <summary>
    ''' The style is validated even when the text has nothing to convert.
    ''' </summary>
    <Fact>
    Public Sub NormalizeLineEndings_UndefinedStyle_ThrowsEvenForEmptyText()

        Assert.Throws(Of InvalidEnumArgumentException)(
            Function() StringsHelper.NormalizeLineEndings(String.Empty, CType(42, LineEndingStyle)))

    End Sub

#End Region

#Region " LineEndingStyle Enumeration "

    ''' <summary>
    ''' The names of the enumeration are the values accepted on the command line (and shown in the usage text), 
    ''' so renaming or reordering them is a breaking change that must be deliberate.
    ''' </summary>
    <Fact>
    Public Sub LineEndingStyle_Members_AreTheDocumentedCommandLineValues()

        Assert.Equal(New String() {"CRLF", "LF", "CR"}, [Enum].GetNames(GetType(LineEndingStyle)))

    End Sub

#End Region

End Class

#End Region
