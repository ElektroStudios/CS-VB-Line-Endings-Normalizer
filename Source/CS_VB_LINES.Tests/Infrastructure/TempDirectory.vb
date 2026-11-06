#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.IO
Imports System.Text
Imports System.Threading

#End Region

#Region " TempDirectory "

''' <summary>
''' Creates a unique temporary directory that is deleted (with all its contents) when the instance is disposed.
''' <para></para>
''' Every test that touches the file system must use its own instance, so the tests never share state 
''' and can safely run in parallel.
''' </summary>
Friend NotInheritable Class TempDirectory
    Implements IDisposable

#Region " Properties "

    ''' <summary>
    ''' Gets the full path of the temporary directory.
    ''' </summary>
    Public ReadOnly Property FullPath As String

#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="TempDirectory"/> class, creating the directory on disk.
    ''' </summary>
    Public Sub New()

        Me.FullPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"CS_VB_Tests_{Guid.NewGuid():N}")
        Directory.CreateDirectory(Me.FullPath)

    End Sub

#End Region

#Region " Public Methods "

    ''' <summary>
    ''' Returns the full path of an item inside the temporary directory.
    ''' </summary>
    ''' 
    ''' <param name="relativePath">
    ''' The path of the item, relative to the temporary directory.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The full path.
    ''' </returns>
    Public Function GetPath(relativePath As String) As String

        Return System.IO.Path.Combine(Me.FullPath, relativePath)

    End Function

    ''' <summary>
    ''' Creates a subdirectory (and any missing parent directory).
    ''' </summary>
    ''' 
    ''' <param name="relativePath">
    ''' The path of the subdirectory, relative to the temporary directory.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The full path of the subdirectory.
    ''' </returns>
    Public Function CreateSubdirectory(relativePath As String) As String

        Dim fullSubdirectoryPath As String = Me.GetPath(relativePath)
        Directory.CreateDirectory(fullSubdirectoryPath)

        Return fullSubdirectoryPath

    End Function

    ''' <summary>
    ''' Writes a text file, using the specified encoding (a preamble is written if the encoding has one), 
    ''' creating any missing parent directory.
    ''' </summary>
    ''' 
    ''' <param name="relativePath">
    ''' The path of the file, relative to the temporary directory.
    ''' </param>
    ''' 
    ''' <param name="text">
    ''' The text to write.
    ''' </param>
    ''' 
    ''' <param name="encoding">
    ''' The encoding.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The full path of the file.
    ''' </returns>
    Public Function WriteText(relativePath As String, text As String, encoding As Encoding) As String

        Dim fullFilePath As String = Me.GetPath(relativePath)
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullFilePath))
        File.WriteAllText(fullFilePath, text, encoding)

        Return fullFilePath

    End Function

    ''' <summary>
    ''' Writes a binary file, creating any missing parent directory.
    ''' </summary>
    ''' 
    ''' <param name="relativePath">
    ''' The path of the file, relative to the temporary directory.
    ''' </param>
    ''' 
    ''' <param name="bytes">
    ''' The bytes to write.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The full path of the file.
    ''' </returns>
    Public Function WriteBytes(relativePath As String, bytes As Byte()) As String

        Dim fullFilePath As String = Me.GetPath(relativePath)
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullFilePath))
        File.WriteAllBytes(fullFilePath, bytes)

        Return fullFilePath

    End Function

    ''' <summary>
    ''' Reads all the bytes of a file of the temporary directory.
    ''' </summary>
    ''' 
    ''' <param name="relativePath">
    ''' The path of the file, relative to the temporary directory.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The bytes of the file.
    ''' </returns>
    Public Function ReadBytes(relativePath As String) As Byte()

        Return File.ReadAllBytes(Me.GetPath(relativePath))

    End Function

    ''' <summary>
    ''' Returns the names (relative to the temporary directory, using '/' as separator) of every file 
    ''' in the temporary directory tree, sorted ordinally.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' The sorted relative file names.
    ''' </returns>
    Public Function GetAllFileNames() As String()

        Dim names As New System.Collections.Generic.List(Of String)()

        For Each filePath As String In Directory.EnumerateFiles(Me.FullPath, "*", SearchOption.AllDirectories)
            names.Add(System.IO.Path.GetRelativePath(Me.FullPath, filePath).Replace("\"c, "/"c))
            Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
        Next filePath

        names.Sort(StringComparer.Ordinal)

        Return names.ToArray()

    End Function

#End Region

#Region " IDisposable Implementation "

    ''' <summary>
    ''' Deletes the temporary directory and all its contents. 
    ''' <para></para>
    ''' The deletion is best-effort on purpose: a failure while cleaning up must never hide the result of a test.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose

        Try
            If Directory.Exists(Me.FullPath) Then
                Directory.Delete(Me.FullPath, recursive:=True)
            End If

        Catch ex As IOException
            ' Best-effort cleanup.

        Catch ex As UnauthorizedAccessException
            ' Best-effort cleanup.

        End Try

    End Sub

#End Region

End Class

#End Region
