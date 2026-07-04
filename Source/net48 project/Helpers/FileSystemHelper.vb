#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Threading

#End Region

#Region " FileSystemHelper "

Friend Module FileSystemHelper

#Region " Static Methods "

    ''' <summary>
    ''' Recursively enumerates all "*.cs" and "*.vb" files under <paramref name="rootDirectoryPath"/>, 
    ''' skipping any subdirectory that cannot be accessed instead of aborting the whole enumeration.
    ''' <para></para>
    ''' This replaces a plain <see cref="Directory.EnumerateFiles(String, String, SearchOption)"/> call with
    ''' <see cref="SearchOption.AllDirectories"/>, which throws (and stops enumerating everything) the moment
    ''' a single protected/system subdirectory is encountered (e.g. "$RECYCLE.BIN", "System Volume Information",
    ''' a folder without read permissions, etc).
    ''' <para></para>
    ''' Traversal is implemented with an explicit stack (breadth/depth order is not guaranteed nor relevant here) 
    ''' instead of recursion, to avoid any risk of stack overflow on pathologically deep directory trees.
    ''' </summary>
    ''' 
    ''' <param name="rootDirectoryPath">
    ''' The root directory path to start the recursive enumeration from.
    ''' </param>
    ''' 
    ''' <param name="searchOption">
    ''' One of the enumeration values that specifies whether the search operation should include all subdirectories 
    ''' or only the current directory.
    ''' </param>
    '''
    ''' <returns>
    ''' A lazily-evaluated sequence of full file paths for every "*.cs" and "*.vb" file found.
    ''' </returns>
    <DebuggerStepThrough>
    Friend Iterator Function EnumerateSourceFiles(rootDirectoryPath As String, searchOption As SearchOption) As IEnumerable(Of String)

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNullOrWhiteSpace(rootDirectoryPath, NameOf(rootDirectoryPath))
#Else
        If String.IsNullOrWhiteSpace(rootDirectoryPath) Then
            Throw New ArgumentNullException(NameOf(rootDirectoryPath))
        End If
#End If

        If Not Directory.Exists(rootDirectoryPath) Then
            Throw New DirectoryNotFoundException($"The specified root directory does not exist: {rootDirectoryPath}")
        End If

        If searchOption <> SearchOption.TopDirectoryOnly AndAlso
           searchOption <> SearchOption.AllDirectories Then

            Throw New InvalidEnumArgumentException(NameOf(searchOption), searchOption, GetType(SearchOption))
        End If

        Dim pendingDirectories As New Stack(Of String)()
        pendingDirectories.Push(rootDirectoryPath)

        Do While pendingDirectories.Count > 0

            Dim currentDirectory As String = pendingDirectories.Pop()

            ' Step 1: List files in the current directory only (non-recursive).
            Dim filesInCurrentDirectory As String() = Nothing
            Try
                filesInCurrentDirectory =
                    Directory.GetFiles(currentDirectory, $"*{Program.CsFileExtension}", SearchOption.TopDirectoryOnly).Concat(
                    Directory.GetFiles(currentDirectory, $"*{Program.VbFileExtension}", SearchOption.TopDirectoryOnly)).ToArray()

            Catch ex As UnauthorizedAccessException
                ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] Access denied to directory: {currentDirectory}", ConsoleColor.DarkGray)

            Catch ex As DirectoryNotFoundException
                ' The directory may have been deleted/moved concurrently between enumeration steps; ignore silently.

            Catch ex As IOException
                ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] I/O error while listing files in directory: {currentDirectory} — {ex.Message}", ConsoleColor.DarkGray)

            Catch ex As Exception
                ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] Error while listing files in directory: {currentDirectory} — {ex.Message}", ConsoleColor.DarkGray)

            End Try

            If filesInCurrentDirectory IsNot Nothing Then

                For Each filePath As String In filesInCurrentDirectory

                    If Path.HasExtension(filePath) Then
                        Dim extension As String = Path.GetExtension(filePath)
                        If extension.Equals(Program.CsFileExtension, StringComparison.OrdinalIgnoreCase) OrElse
                           extension.Equals(Program.VbFileExtension, StringComparison.OrdinalIgnoreCase) Then
                            Yield filePath
                        End If
                    End If

#If DEBUG Then
                    Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
#End If
                Next filePath
            End If

            If searchOption = SearchOption.AllDirectories Then

                ' Step 2: Queue subdirectories for later traversal.
                Dim subDirectories As String() = Nothing
                Try
                    subDirectories = Directory.GetDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly)

                Catch ex As UnauthorizedAccessException
                    ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] Access denied to directory: {currentDirectory}", ConsoleColor.DarkGray)

                Catch ex As DirectoryNotFoundException
                    ' The directory may have been deleted/moved concurrently between enumeration steps; ignore silently.

                Catch ex As IOException
                    ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] I/O error while listing files in directory: {currentDirectory} — {ex.Message}", ConsoleColor.DarkGray)

                Catch ex As Exception
                    ConsoleHelper.WriteColoredTextLine($"[SKIPPED DIRECTORY] Error while listing files in directory: {currentDirectory} — {ex.Message}", ConsoleColor.DarkGray)

                End Try

                If subDirectories IsNot Nothing Then

                    For Each subDirectory As String In subDirectories
                        pendingDirectories.Push(subDirectory)
                        Thread.CurrentThread.Join(0) ' Prevents ContextSwitchDeadlock on long-running iterations.
                    Next subDirectory

                End If

            End If

        Loop ' While pendingDirectories.Count > 0

    End Function

    ''' <summary>
    ''' Reads and returns up to <paramref name="maxBytes"/> bytes from the start of the specified file.
    ''' </summary>
    ''' 
    ''' <param name="filePath">
    ''' The path of the file to read.
    ''' </param>
    ''' 
    ''' <param name="maxBytes">
    ''' The maximum number of bytes to read from the file.
    ''' </param>
    ''' 
    ''' <param name="throwIfMaxBytesExceedsFileSize">
    ''' Optional. If set to <see langword="True"/>, an exception is thrown when the file size is strictly less than <paramref name="maxBytes"/>.
    ''' <para></para>
    ''' Default value is <see langword="False"/>, in which case the function will read all available bytes if the file size is smaller than <paramref name="maxBytes"/>.
    ''' </param>
    '''
    ''' <returns>
    ''' A byte array containing the bytes actually read from the file. 
    ''' <para></para>
    ''' The length of the array will match <paramref name="maxBytes"/>, 
    ''' unless the file size is smaller or the end of the stream is reached prematurely, 
    ''' in which case it returns all available bytes.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="filePath"/> is null, empty, or consists only of whitespace characters.
    ''' </exception>
    ''' 
    ''' <exception cref="ArgumentOutOfRangeException">
    ''' Thrown when <paramref name="maxBytes"/> is less than or equal to zero.
    ''' </exception>
    ''' 
    ''' <exception cref="FileNotFoundException">
    ''' Thrown when the file specified in <paramref name="filePath"/> cannot be found.
    ''' </exception>
    ''' 
    ''' <exception cref="InvalidOperationException">
    ''' Thrown when <paramref name="throwIfMaxBytesExceedsFileSize"/> is <see langword="True"/> and the file length is less than <paramref name="maxBytes"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Public Function ReadSampleBytes(filePath As String,
                                    maxBytes As Integer,
                           Optional throwIfMaxBytesExceedsFileSize As Boolean = False) As Byte()

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath, NameOf(filePath))
#Else
        If String.IsNullOrWhiteSpace(filePath) Then
            Throw New ArgumentNullException(NameOf(filePath))
        End If
#End If

        If maxBytes <= 0 Then
            Dim exceptionMaxBytesMessage As String = $"The parameter {NameOf(maxBytes)} must be greater than zero."
            Throw New ArgumentOutOfRangeException(NameOf(maxBytes), exceptionMaxBytesMessage)
        End If

        Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)

            If throwIfMaxBytesExceedsFileSize AndAlso fs.Length < maxBytes Then
                Dim invalidOpMessage As String = $"The file size ({fs.Length} bytes) is less than the requested maximum bytes ({maxBytes})."
                Throw New InvalidOperationException(invalidOpMessage)
            End If

            Dim sampleSize As Integer = CInt(Math.Min(maxBytes, fs.Length))
            Dim buffer(sampleSize - 1) As Byte

            Dim totalBytesRead As Integer = 0
            Do While totalBytesRead < sampleSize

                Dim bytesReadThisCall As Integer = fs.Read(buffer, totalBytesRead, sampleSize - totalBytesRead)
                If bytesReadThisCall = 0 Then
                    Exit Do
                End If

                totalBytesRead += bytesReadThisCall
            Loop

            If totalBytesRead < buffer.Length Then
                ReDim Preserve buffer(totalBytesRead - 1)
            End If

            Return buffer
        End Using

    End Function

    ''' <summary>
    ''' Converts a standard path into an Extended-Length Path (prefixed with \\?\) to bypass the 260 character MAX_PATH limitation.
    ''' </summary>
    ''' 
    ''' <param name="targetPath">
    ''' The absolute path to convert.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The extended-length path string.
    ''' </returns>
    <DebuggerStepThrough>
    Friend Function GetExtendedPath(targetPath As String) As String

        If String.IsNullOrWhiteSpace(targetPath) Then
            Return targetPath
        End If

        ' Already an extended path.
        If targetPath.StartsWith("\\?\", StringComparison.Ordinal) Then
            Return targetPath
        End If

        ' Relative paths cannot be converted to Extended-Length paths.
        If Not Path.IsPathRooted(targetPath) Then
            Return targetPath
        End If

        ' Handle UNC paths: \\Server\Share -> \\?\UNC\Server\Share
        If targetPath.StartsWith("\\", StringComparison.Ordinal) Then
            Return $"\\?\UNC\{targetPath.Substring(2)}"
        End If

        ' Handle Local paths: C:\Folder -> \\?\C:\Folder
        Return $"\\?\{targetPath}"
    End Function

    ''' <summary>
    ''' Strips the Extended-Length Path prefix for UI display or Shell interop.
    ''' </summary>
    <DebuggerStepThrough>
    Friend Function GetNormalPath(targetPath As String) As String

        Return If(String.IsNullOrWhiteSpace(targetPath),
            targetPath,
            If(targetPath.StartsWith("\\?\UNC\", StringComparison.OrdinalIgnoreCase),
            $"\\{targetPath.Substring(8)}",
            If(targetPath.StartsWith("\\?\", StringComparison.Ordinal), targetPath.Substring(4), targetPath)))
    End Function

#End Region

End Module

#End Region
