#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics

#End Region

#Region " ConsoleHelper "

Friend Module ConsoleHelper

#Region " Static Methods "

    ''' <summary>
    ''' Writes a message to the console in a specified foreground color, 
    ''' then resets the color back to the original.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Friend Sub WriteColoredText(message As String, foreColor As ConsoleColor)

        Dim originalForeColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = foreColor
        Console.Write(message)
        Console.ForegroundColor = originalForeColor
    End Sub

    ''' <summary>
    ''' Writes a message to the console in a specified foreground color, 
    ''' then resets the color back to the original.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Friend Sub WriteColoredTextLine(message As String, foreColor As ConsoleColor)

        ConsoleHelper.WriteColoredText(message & Environment.NewLine, foreColor)
    End Sub

    ''' <summary>
    ''' Displays a message to the console and exits the application with the specified exit code.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display before exiting. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="exitCode">
    ''' The exit code to return to the operating system. Typically 0 for success, non-zero for errors.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Friend Sub ExitWithMessage(message As String, exitCode As Integer, foreColor As ConsoleColor)

        If Not String.IsNullOrEmpty(message) Then
            ConsoleHelper.WriteColoredTextLine(message, foreColor)
            Console.WriteLine()
        End If

        Console.WriteLine($"Exiting application with exit code: {exitCode} (0x{exitCode}) ...")
#If DEBUG Then
        Console.WriteLine()
        ConsoleHelper.WriteColoredTextLine("[!] This message only appears in DEBUG mode to prevent accidental termination.", ConsoleColor.Yellow)
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey(intercept:=True)
#End If
        Environment.Exit(exitCode)
    End Sub

#End Region

End Module

#End Region
