#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " LineEndingStyle "

''' <summary>
''' Defines the supported line-ending normalization target styles.
''' </summary>
Public Enum LineEndingStyle

    ''' <summary>
    ''' Carriage Return + Line Feed ("\r\n"). Standard on Windows.
    ''' </summary>
    CRLF

    ''' <summary>
    ''' Line Feed only ("\n"). Standard on Linux/macOS, and in most Git repositories.
    ''' </summary>
    LF

    ''' <summary>
    ''' Carriage Return only ("\r"). Legacy classic Mac OS (pre-OS X) style. Rarely used nowadays.
    ''' </summary>
    CR

End Enum

#End Region
