#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System
Imports System.Text

#End Region

#Region " TestEncodings "

''' <summary>
''' Creates the encodings supported by the application, from the short names that the data-driven tests use 
''' (the same names as the members of the "UtfEncodingKind" enumeration), and encodes texts the way an editor saves them.
''' </summary>
Friend Module TestEncodings

    ''' <summary>
    ''' The short names of every encoding variant supported by the application, UTF-7 excluded.
    ''' </summary>
    Friend ReadOnly AllNames As String() = {
        "UTF8", "UTF8_Bom",
        "UTF16LE", "UTF16LE_Bom", "UTF16BE", "UTF16BE_Bom",
        "UTF32LE", "UTF32LE_Bom", "UTF32BE", "UTF32BE_Bom"
    }

    ''' <summary>
    ''' Creates an encoding from its short name.
    ''' </summary>
    ''' 
    ''' <param name="name">
    ''' The short name, for example "UTF8", "UTF16LE_Bom" or "UTF32BE".
    ''' </param>
    ''' 
    ''' <returns>
    ''' The encoding, which emits a preamble only when the name ends with "_Bom".
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentException">
    ''' The name is unknown.
    ''' </exception>
    Friend Function Create(name As String) As Encoding

        Select Case name
            Case "UTF8"
                Return New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)
            Case "UTF8_Bom"
                Return New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)
            Case "UTF16LE"
                Return New UnicodeEncoding(bigEndian:=False, byteOrderMark:=False)
            Case "UTF16LE_Bom"
                Return New UnicodeEncoding(bigEndian:=False, byteOrderMark:=True)
            Case "UTF16BE"
                Return New UnicodeEncoding(bigEndian:=True, byteOrderMark:=False)
            Case "UTF16BE_Bom"
                Return New UnicodeEncoding(bigEndian:=True, byteOrderMark:=True)
            Case "UTF32LE"
                Return New UTF32Encoding(bigEndian:=False, byteOrderMark:=False)
            Case "UTF32LE_Bom"
                Return New UTF32Encoding(bigEndian:=False, byteOrderMark:=True)
            Case "UTF32BE"
                Return New UTF32Encoding(bigEndian:=True, byteOrderMark:=False)
            Case "UTF32BE_Bom"
                Return New UTF32Encoding(bigEndian:=True, byteOrderMark:=True)
            Case Else
                Throw New ArgumentException($"Unknown encoding name: {name}", NameOf(name))
        End Select

    End Function

    ''' <summary>
    ''' Encodes a text with the preamble of the encoding (if it has one), as a file saved by an editor would be.
    ''' </summary>
    ''' 
    ''' <param name="text">
    ''' The text.
    ''' </param>
    ''' 
    ''' <param name="encoding">
    ''' The encoding.
    ''' </param>
    ''' 
    ''' <returns>
    ''' The bytes of the preamble followed by the bytes of the text.
    ''' </returns>
    Friend Function Encode(text As String, encoding As Encoding) As Byte()

        Dim preamble As Byte() = encoding.GetPreamble()
        Dim body As Byte() = encoding.GetBytes(text)
        Dim result(preamble.Length + body.Length - 1) As Byte

        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length)
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length)

        Return result

    End Function

End Module

#End Region
