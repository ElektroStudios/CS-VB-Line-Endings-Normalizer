#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Diagnostics

Imports Microsoft.VisualBasic

#End Region

#Region " StringsHelper "

Friend Module StringsHelper

#Region " Static Methods "

    ''' <summary>
    ''' Normalizes all line endings in the given string to the specified target line ending style. 
    ''' </summary>
    ''' 
    ''' <param name="text">
    ''' The original text to normalize, possibly containing a mix of line ending styles.
    ''' </param>
    ''' 
    ''' <param name="targetStyle">
    ''' The line ending style to normalize to.
    ''' </param>
    '''
    ''' <returns>
    ''' A new string with all line endings normalized to <paramref name="targetStyle"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="text"/> is <see langword="Nothing"/>.
    ''' </exception>
    ''' 
    ''' <exception cref="InvalidEnumArgumentException">
    ''' Thrown when <paramref name="targetStyle"/> is not a valid <see cref="LineEndingStyle"/> value.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function NormalizeLineEndings(text As String, targetStyle As LineEndingStyle) As String

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(text)
#Else
        If text Is Nothing Then
            Throw New ArgumentNullException(NameOf(text))
        End If
#End If

        Dim canonicalLf As String =
            StringsHelper.CompatibleReplace(
                StringsHelper.CompatibleReplace(text, Constants.vbCrLf, Constants.vbLf),
                Constants.vbCr, Constants.vbLf)

        Select Case targetStyle

            Case LineEndingStyle.CRLF
                Return StringsHelper.CompatibleReplace(canonicalLf, Constants.vbLf, Constants.vbCrLf)

            Case LineEndingStyle.LF
                Return canonicalLf

            Case LineEndingStyle.CR
                Return StringsHelper.CompatibleReplace(canonicalLf, Constants.vbLf, Constants.vbCr)

            Case Else
                Throw New InvalidEnumArgumentException(NameOf(targetStyle), targetStyle, GetType(LineEndingStyle))

        End Select

    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Returns a new string in which all occurrences of a specified string in <paramref name="source"/>
    ''' are replaced with another specified string, using ordinal comparison.
    ''' </summary>
    ''' 
    ''' <param name="source">
    ''' The source string to perform the replacement on.
    ''' </param>
    ''' 
    ''' <param name="oldValue">
    ''' The string to be replaced.
    ''' </param>
    ''' 
    ''' <param name="newValue">
    ''' The string to replace all occurrences of <paramref name="oldValue"/>. 
    ''' If <see langword="Nothing"/>, all occurrences of <paramref name="oldValue"/> are removed.
    ''' </param>
    ''' 
    ''' <returns>
    ''' A string that is equivalent to <paramref name="source"/> except that all instances of 
    ''' <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="source"/> or <paramref name="oldValue"/> is <see langword="Nothing"/>.
    ''' </exception>
    ''' 
    ''' <exception cref="ArgumentException">
    ''' Thrown when <paramref name="oldValue"/> is an empty string.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function CompatibleReplace(source As String, oldValue As String, newValue As String) As String

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(source)
        ArgumentNullException.ThrowIfNull(oldValue)
#Else
        If source Is Nothing Then
            Throw New ArgumentNullException(NameOf(source))
        End If

        If oldValue Is Nothing Then
            Throw New ArgumentNullException(NameOf(oldValue))
        End If
#End If

        If oldValue.Length = 0 Then
            Throw New ArgumentException("Value cannot be an empty string.", NameOf(oldValue))
        End If

#If NETCOREAPP Then
        Return source.Replace(oldValue, newValue, StringComparison.Ordinal)
#Else
        Return source.Replace(oldValue, newValue)
#End If

    End Function

#End Region

End Module

#End Region
