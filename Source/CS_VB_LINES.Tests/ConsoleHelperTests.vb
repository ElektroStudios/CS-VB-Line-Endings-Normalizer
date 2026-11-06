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

#Region " ConsoleHelperTests "

''' <summary>
''' Tests for <see cref="ConsoleHelper"/>. 
''' <para></para>
''' <see cref="ConsoleHelper.ExitWithMessage"/> terminates the process, so it is covered by the end-to-end tests of the entry point.
''' <para></para>
''' These tests replace the standard output of the process, so they belong to a collection 
''' that is never run in parallel with other tests of the same collection.
''' </summary>
<Collection("Console output")>
Public Class ConsoleHelperTests
    Implements IDisposable

#Region " Fields "

    ''' <summary>
    ''' The standard output writer that was set before the test, restored when the test ends.
    ''' </summary>
    Private ReadOnly originalOutput As TextWriter = Console.Out

    ''' <summary>
    ''' The writer that captures what the helper writes.
    ''' </summary>
    Private ReadOnly captured As New StringWriter()

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ConsoleHelperTests"/> class, redirecting the console output to a writer.
    ''' </summary>
    Public Sub New()

        Console.SetOut(Me.captured)

    End Sub

#End Region

#Region " WriteColoredText "

    <Fact>
    Public Sub WriteColoredText_WritesTheMessageWithoutAddingALineBreak()

        ConsoleHelper.WriteColoredText("Hello", ConsoleColor.Green)

        Assert.Equal("Hello", Me.captured.ToString())

    End Sub

    <Fact>
    Public Sub WriteColoredText_EmptyMessage_WritesNothing()

        ConsoleHelper.WriteColoredText(String.Empty, ConsoleColor.Red)

        Assert.Equal(String.Empty, Me.captured.ToString())

    End Sub

    ''' <summary>
    ''' The console color after the call must be the one that was set before it, whatever color was used.
    ''' </summary>
    <Theory>
    <InlineData(ConsoleColor.Red)>
    <InlineData(ConsoleColor.Cyan)>
    <InlineData(ConsoleColor.Black)>
    <InlineData(ConsoleColor.White)>
    Public Sub WriteColoredText_RestoresTheOriginalForegroundColor(color As ConsoleColor)

        Dim before As ConsoleColor = Console.ForegroundColor

        ConsoleHelper.WriteColoredText("text", color)

        Assert.Equal(before, Console.ForegroundColor)

    End Sub

    <Fact>
    Public Sub WriteColoredText_ConsecutiveCalls_AreWrittenInOrder()

        ConsoleHelper.WriteColoredText("A", ConsoleColor.Red)
        ConsoleHelper.WriteColoredText("B", ConsoleColor.Green)
        ConsoleHelper.WriteColoredText("C", ConsoleColor.Blue)

        Assert.Equal("ABC", Me.captured.ToString())

    End Sub

    ''' <summary>
    ''' Unicode text (the application draws boxes with box-drawing characters) and text with braces or percent signs 
    ''' is written literally, as it is never used as a format string.
    ''' </summary>
    <Fact>
    Public Sub WriteColoredText_UnicodeAndFormatLikeText_IsWrittenLiterally()

        Dim message As String = "╭─ {0} {name} 100% %s ─╮ ñ 日本語"

        ConsoleHelper.WriteColoredText(message, ConsoleColor.Gray)

        Assert.Equal(message, Me.captured.ToString())

    End Sub

#End Region

#Region " WriteColoredTextLine "

    <Fact>
    Public Sub WriteColoredTextLine_AppendsTheEnvironmentNewLine()

        ConsoleHelper.WriteColoredTextLine("Hello", ConsoleColor.Yellow)

        Assert.Equal($"Hello{Environment.NewLine}", Me.captured.ToString())

    End Sub

    <Fact>
    Public Sub WriteColoredTextLine_EmptyMessage_WritesOnlyTheLineBreak()

        ConsoleHelper.WriteColoredTextLine(String.Empty, ConsoleColor.Yellow)

        Assert.Equal(Environment.NewLine, Me.captured.ToString())

    End Sub

    <Fact>
    Public Sub WriteColoredTextLine_RestoresTheOriginalForegroundColor()

        Dim before As ConsoleColor = Console.ForegroundColor

        ConsoleHelper.WriteColoredTextLine("text", ConsoleColor.Magenta)

        Assert.Equal(before, Console.ForegroundColor)

    End Sub

    <Fact>
    Public Sub WriteColoredTextLine_MessageWithItsOwnLineBreaks_KeepsThem()

        ConsoleHelper.WriteColoredTextLine($"line 1{vbLf}line 2", ConsoleColor.Gray)

        Assert.Equal($"line 1{vbLf}line 2{Environment.NewLine}", Me.captured.ToString())

    End Sub

#End Region

#Region " IDisposable Implementation "

    ''' <summary>
    ''' Restores the original standard output.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose

        Console.SetOut(Me.originalOutput)
        Me.captured.Dispose()

    End Sub

#End Region

End Class

#End Region
