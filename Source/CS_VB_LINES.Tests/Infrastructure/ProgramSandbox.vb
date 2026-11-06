#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.Collections.Generic

#End Region

#Region " ProgramSandbox "

''' <summary>
''' An isolated environment for one end-to-end test: a directory with the source files that the application will process, 
''' and a separate scratch directory that the application uses as its temporary directory, 
''' so test files and application temporary files never get mixed.
''' </summary>
Friend NotInheritable Class ProgramSandbox
    Implements IDisposable

#Region " Properties "

    ''' <summary>
    ''' Gets the directory that contains the files processed by the application.
    ''' </summary>
    Public ReadOnly Property Source As TempDirectory

    ''' <summary>
    ''' Gets the directory that the application uses as its temporary directory.
    ''' </summary>
    Public ReadOnly Property Scratch As TempDirectory

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ProgramSandbox"/> class.
    ''' </summary>
    Public Sub New()

        Me.Source = New TempDirectory()
        Me.Scratch = New TempDirectory()

    End Sub

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Runs the application on the source directory.
    ''' </summary>
    ''' 
    ''' <param name="styleArgument">
    ''' The text passed as the line-ending style argument.
    ''' </param>
    ''' 
    ''' <param name="options">
    ''' The optional switches, such as "-r" or "--test".
    ''' </param>
    ''' 
    ''' <returns>
    ''' The exit code and the captured output.
    ''' </returns>
    Public Function Run(styleArgument As String, ParamArray options As String()) As ProgramRunResult

        Dim arguments As New List(Of String) From {Me.Source.FullPath, styleArgument}
        arguments.AddRange(options)

        Return ProgramRunner.Run(arguments, Me.Scratch.FullPath)

    End Function

    ''' <summary>
    ''' Runs the application with exactly the specified arguments.
    ''' </summary>
    ''' 
    ''' <param name="arguments">
    ''' The arguments.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The exit code and the captured output.
    ''' </returns>
    Public Function RunRaw(ParamArray arguments As String()) As ProgramRunResult

        Return ProgramRunner.Run(arguments, Me.Scratch.FullPath)

    End Function

#End Region

#Region " IDisposable Implementation "

    ''' <summary>
    ''' Deletes both directories.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose

        Me.Source.Dispose()
        Me.Scratch.Dispose()

    End Sub

#End Region

End Class

#End Region
