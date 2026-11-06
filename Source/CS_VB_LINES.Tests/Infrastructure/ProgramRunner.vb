#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks

#End Region

#Region " ProgramRunResult "

''' <summary>
''' The outcome of a run of the application executed as a separate process.
''' </summary>
Friend NotInheritable Class ProgramRunResult

    ''' <summary>
    ''' Gets the exit code of the process.
    ''' </summary>
    Public ReadOnly Property ExitCode As Integer

    ''' <summary>
    ''' Gets everything that the process wrote to the standard output stream.
    ''' </summary>
    Public ReadOnly Property StandardOutput As String

    ''' <summary>
    ''' Gets everything that the process wrote to the standard error stream.
    ''' </summary>
    Public ReadOnly Property StandardError As String

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ProgramRunResult"/> class.
    ''' </summary>
    ''' 
    ''' <param name="exitCode">
    ''' The exit code of the process.
    ''' </param>
    ''' 
    ''' <param name="standardOutput">
    ''' The text written to the standard output stream.
    ''' </param>
    ''' 
    ''' <param name="standardError">
    ''' The text written to the standard error stream.
    ''' </param>
    Public Sub New(exitCode As Integer, standardOutput As String, standardError As String)

        Me.ExitCode = exitCode
        Me.StandardOutput = standardOutput
        Me.StandardError = standardError

    End Sub

    ''' <summary>
    ''' Returns a text with the exit code and the captured output, to be used as the failure message of an assertion.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' The diagnostic text.
    ''' </returns>
    Public Overrides Function ToString() As String

        Return $"Exit code: {Me.ExitCode}{Environment.NewLine}--- STDOUT ---{Environment.NewLine}{Me.StandardOutput}{Environment.NewLine}--- STDERR ---{Environment.NewLine}{Me.StandardError}"

    End Function

End Class

#End Region

#Region " ProgramRunner "

''' <summary>
''' Runs the real application (the "CS-VB_LINES" assembly that is copied next to the test assembly) as a separate process,
''' so the entry point can be tested end-to-end, including its exit codes, 
''' even though it terminates the process by calling <see cref="Environment.[Exit]"/>.
''' <para></para>
''' IMPORTANT: the application must be built in Release configuration, because the Debug build waits for key presses. 
''' The test project forces that on its project reference.
''' </summary>
Friend NotInheritable Class ProgramRunner

#Region " Constants "

    ''' <summary>
    ''' The file name of the application assembly.
    ''' </summary>
    Private Const AppAssemblyFileName As String = "CS-VB_LINES.dll"

    ''' <summary>
    ''' The maximum time that a run of the application is allowed to last, in milliseconds.
    ''' </summary>
    Private Const TimeoutMilliseconds As Integer = 60000

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Prevents a default instance of the <see cref="ProgramRunner"/> class from being created.
    ''' </summary>
    Private Sub New()
    End Sub

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Runs the application with the specified arguments and waits for it to exit.
    ''' </summary>
    ''' 
    ''' <param name="arguments">
    ''' The command-line arguments. Each element is passed as a single argument (no quoting is needed).
    ''' </param>
    ''' 
    ''' <param name="tempDirectoryPath">
    ''' Optional. The directory that the process will use as its temporary directory, so it never touches the real one.
    ''' </param>
    ''' 
    ''' <param name="workingDirectoryPath">
    ''' Optional. The working directory of the process.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The exit code and the captured output of the process.
    ''' </returns>
    ''' 
    ''' <exception cref="TimeoutException">
    ''' The process did not exit in time.
    ''' </exception>
    Public Shared Function Run(arguments As IEnumerable(Of String),
                      Optional tempDirectoryPath As String = Nothing,
                      Optional workingDirectoryPath As String = Nothing) As ProgramRunResult

        Dim appAssemblyPath As String = Path.Combine(AppContext.BaseDirectory, AppAssemblyFileName)
        If Not File.Exists(appAssemblyPath) Then
            Throw New FileNotFoundException($"The application assembly was not found next to the test assembly: {appAssemblyPath}", appAssemblyPath)
        End If

        Dim startInfo As New ProcessStartInfo() With {
            .FileName = ProgramRunner.GetDotnetHostPath(),
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardInput = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .StandardOutputEncoding = Encoding.UTF8,
            .StandardErrorEncoding = Encoding.UTF8
        }

        startInfo.ArgumentList.Add(appAssemblyPath)
        For Each argument As String In arguments
            startInfo.ArgumentList.Add(argument)
        Next argument

        If Not String.IsNullOrEmpty(workingDirectoryPath) Then
            startInfo.WorkingDirectory = workingDirectoryPath
        End If

        If Not String.IsNullOrEmpty(tempDirectoryPath) Then
            startInfo.Environment("TMPDIR") = tempDirectoryPath ' Unix.
            startInfo.Environment("TMP") = tempDirectoryPath    ' Windows.
            startInfo.Environment("TEMP") = tempDirectoryPath   ' Windows.
        End If

        Using process As Process = Process.Start(startInfo)

            process.StandardInput.Close() ' The application must never wait for user input.

            Dim standardOutputTask As Task(Of String) = process.StandardOutput.ReadToEndAsync()
            Dim standardErrorTask As Task(Of String) = process.StandardError.ReadToEndAsync()

            If Not process.WaitForExit(TimeoutMilliseconds) Then
                process.Kill(entireProcessTree:=True)
                Throw New TimeoutException($"The application did not exit within {TimeoutMilliseconds:N0} ms.")
            End If

            process.WaitForExit() ' Ensures that the redirected streams have been completely flushed.

            Return New ProgramRunResult(process.ExitCode, standardOutputTask.Result, standardErrorTask.Result)

        End Using

    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Gets the path of the "dotnet" host that is used to run the application assembly.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' The path (or just "dotnet", to resolve it through the PATH environment variable).
    ''' </returns>
    Private Shared Function GetDotnetHostPath() As String

        Dim currentProcessPath As String = Environment.ProcessPath
        If Not String.IsNullOrEmpty(currentProcessPath) AndAlso
           Path.GetFileNameWithoutExtension(currentProcessPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase) Then

            Return currentProcessPath
        End If

        Dim hostPathFromEnvironment As String = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")
        If Not String.IsNullOrEmpty(hostPathFromEnvironment) AndAlso File.Exists(hostPathFromEnvironment) Then
            Return hostPathFromEnvironment
        End If

        Return "dotnet"

    End Function

#End Region

End Class

#End Region
