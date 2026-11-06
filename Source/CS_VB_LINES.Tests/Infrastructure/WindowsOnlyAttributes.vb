#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System

Imports Xunit

#End Region

#Region " WindowsOnlyFactAttribute "

''' <summary>
''' Marks a test method that relies on Windows-only behavior (drive letters, UNC paths, extended-length paths, 
''' case-insensitive file system...). 
''' <para></para>
''' On any other operating system the test is reported as skipped instead of failing.
''' </summary>
<AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
Public NotInheritable Class WindowsOnlyFactAttribute
    Inherits FactAttribute

    ''' <summary>
    ''' Initializes a new instance of the <see cref="WindowsOnlyFactAttribute"/> class.
    ''' </summary>
    Public Sub New()

        MyBase.New()

        If Not OperatingSystem.IsWindows() Then
            Me.Skip = "This test requires Windows path semantics."
        End If

    End Sub

End Class

#End Region

#Region " WindowsOnlyTheoryAttribute "

''' <summary>
''' Marks a data-driven test method that relies on Windows-only behavior (drive letters, UNC paths, extended-length paths...). 
''' <para></para>
''' On any other operating system the test is reported as skipped instead of failing.
''' </summary>
<AttributeUsage(AttributeTargets.Method, AllowMultiple:=False)>
Public NotInheritable Class WindowsOnlyTheoryAttribute
    Inherits TheoryAttribute

    ''' <summary>
    ''' Initializes a new instance of the <see cref="WindowsOnlyTheoryAttribute"/> class.
    ''' </summary>
    Public Sub New()

        MyBase.New()

        If Not OperatingSystem.IsWindows() Then
            Me.Skip = "This test requires Windows path semantics."
        End If

    End Sub

End Class

#End Region
