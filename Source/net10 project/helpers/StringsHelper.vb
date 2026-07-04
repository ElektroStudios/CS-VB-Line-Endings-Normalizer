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
    <DebuggerStepThrough>
    Friend Function NormalizeLineEndings(text As String, targetStyle As LineEndingStyle) As String

        Dim canonicalLf As String =
            text.Replace(Constants.vbCrLf, Constants.vbLf).
                       Replace(Constants.vbCr, Constants.vbLf)

        Select Case targetStyle

            Case LineEndingStyle.CRLF
                Return canonicalLf.Replace(Constants.vbLf, Constants.vbCrLf)

            Case LineEndingStyle.LF
                Return canonicalLf

            Case LineEndingStyle.CR
                Return canonicalLf.Replace(Constants.vbLf, Constants.vbCr)

            Case Else
                Throw New InvalidEnumArgumentException(NameOf(targetStyle), targetStyle, GetType(LineEndingStyle))

        End Select

    End Function

#End Region

End Module

#End Region
