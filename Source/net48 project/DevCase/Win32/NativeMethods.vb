
#If NOT NETCOREAPP Then

#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Runtime.InteropServices
Imports System.Security

#End Region

''' <summary>
''' Platform Invocation methods (P/Invoke), access unmanaged code.
''' </summary>
<SuppressUnmanagedCodeSecurity>
Friend Module NativeMethods

#Region " shlwapi.dll "

    <DllImport("shlwapi.dll", SetLastError:=False, CharSet:=CharSet.Unicode, ExactSpelling:=True)>
    Friend Function StrCmpLogicalW(first As String, second As String) As Integer
    End Function

#End Region

End Module

#End If
