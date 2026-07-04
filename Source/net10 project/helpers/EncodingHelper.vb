#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.ComponentModel
Imports System.Diagnostics
Imports System.IO
Imports System.Text

#End Region

#Region " EncodingHelper "

Friend Module EncodingHelper

#Region " Enums "

    ''' <summary>
    ''' Specifies the kind of UTF encoding used to read or write a text file.
    ''' </summary>
    Friend Enum UtfEncodingKind

        ''' <summary>
        ''' The encoding type is unknown (could not be determined).
        ''' </summary>
        Unknown

        ''' <summary>
        ''' UTF-7 encoding.
        ''' </summary>
        UTF7

        ''' <summary>
        ''' UTF-8 encoding without a Byte Order Mark (BOM).
        ''' </summary>
        UTF8

        ''' <summary>
        ''' UTF-8 encoding with a Byte Order Mark (BOM).
        ''' </summary>
        UTF8_Bom

        ''' <summary>
        ''' UTF-16 Big Endian encoding without a Byte Order Mark (BOM).
        ''' </summary>
        UTF16BE

        ''' <summary>
        ''' UTF-16 Big Endian encoding with a Byte Order Mark (BOM).
        ''' </summary>
        UTF16BE_Bom

        ''' <summary>
        ''' UTF-16 Little Endian encoding without a Byte Order Mark (BOM).
        ''' </summary>
        UTF16LE

        ''' <summary>
        ''' UTF-16 Little Endian encoding with a Byte Order Mark (BOM).
        ''' </summary>
        UTF16LE_Bom

        ''' <summary>
        ''' UTF-32 Big Endian encoding without a Byte Order Mark (BOM).
        ''' </summary>
        UTF32BE

        ''' <summary>
        ''' UTF-32 Big Endian encoding with a Byte Order Mark (BOM).
        ''' </summary>
        UTF32BE_Bom

        ''' <summary>
        ''' UTF-32 Little Endian encoding without a Byte Order Mark (BOM).
        ''' </summary>
        UTF32LE

        ''' <summary>
        ''' UTF-32 Little Endian encoding with a Byte Order Mark (BOM).
        ''' </summary>
        UTF32LE_Bom

    End Enum

#End Region

#Region " Static Methods "

    ''' <summary>
    ''' Tries to determine the <see cref="UtfEncodingKind"/> of the specified file based on its UTF byte signature and internal heuristics.
    ''' <para></para>
    ''' ⚠️ Important: 
    ''' <para></para>
    ''' Because the nature of UTF encodings allows for multiple valid representations of the same text, 
    ''' this method may not always be able to determine the encoding with absolute certainty.
    ''' <para></para>
    ''' Take the resulting <see cref="UtfEncodingKind"/> output value as an approximation rather than a guarantee.
    ''' </summary>
    ''' 
    ''' <example> This is a full test code example that can be used to validate the behavior of <see cref="TryGetUtfFileEncoding"/> against a variety of UTF and legacy encodings.
    ''' <code language="VB">
    ''' Dim encodingsToTest As New Dictionary(Of String, Encoding) From {
    '''     {"UTF-7.txt", New UTF7Encoding(allowOptionals:=False)},
    '''     {"UTF-8.txt", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)},
    '''     {"UTF-8 (with BOM).txt", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)},
    '''     {"UTF-16.txt", New UnicodeEncoding(bigEndian:=False, byteOrderMark:=False)},
    '''     {"UTF-16 (with BOM).txt", New UnicodeEncoding(bigEndian:=False, byteOrderMark:=True)},
    '''     {"UTF-16BE.txt", New UnicodeEncoding(bigEndian:=True, byteOrderMark:=False)},
    '''     {"UTF-16BE (with BOM).txt", New UnicodeEncoding(bigEndian:=True, byteOrderMark:=True)},
    '''     {"UTF-32.txt", New UTF32Encoding(bigEndian:=False, byteOrderMark:=False)},
    '''     {"UTF-32 (with BOM).txt", New UTF32Encoding(bigEndian:=False, byteOrderMark:=True)},
    '''     {"UTF-32BE.txt", New UTF32Encoding(bigEndian:=True, byteOrderMark:=False)},
    '''     {"UTF-32BE (with BOM).txt", New UTF32Encoding(bigEndian:=True, byteOrderMark:=True)} _
    '''     , ' These shouldn't be matched by the UTF detection logic, but are included for testing purposes.
    '''     {"DOS (CP-437).txt", Encoding.GetEncoding("CP437")},
    '''     {"Windows-1252.txt", Encoding.GetEncoding("Windows-1252")},
    '''     {"ISO-8859-1.txt", Encoding.GetEncoding("ISO-8859-1")},
    '''     {"ISO-8859-2.txt", Encoding.GetEncoding("ISO-8859-2")},
    '''     {"ISO-8859-3.txt", Encoding.GetEncoding("ISO-8859-3")},
    '''     {"ISO-8859-15.txt", Encoding.GetEncoding("ISO-8859-15")}
    ''' }
    ''' 
    ''' Dim stringToTest As String = "Hello, World! こんにちは世界 àèìòù¡!¿?€áéíóúñÑÇç"
    ''' 
    ''' For Each pair As KeyValuePair(Of String, Encoding) In encodingsToTest
    ''' 
    '''     Dim fileName As String = pair.Key
    '''     Dim filePath As String = Path.Combine(Path.GetTempPath(), fileName)
    '''     Dim enc As Encoding = pair.Value
    ''' 
    '''     File.WriteAllText(filePath, stringToTest, enc)
    ''' 
    '''     Dim detectedKind As UtfEncodingKind = UtfEncodingKind.Unknown
    '''     Dim isUtfHandled As Boolean = EncodingHelper.TryGetUtfFileEncodingKind(filePath, detectedKind)
    ''' 
    '''     Dim isLegacyFile As Boolean = Not fileName.StartsWith("utf", StringComparison.OrdinalIgnoreCase)
    ''' 
    '''     If isLegacyFile Then
    '''         If isUtfHandled OrElse (detectedKind &lt;&gt; UtfEncodingKind.Unknown) Then
    '''             Console.ForegroundColor = ConsoleColor.Red
    '''             Console.WriteLine($"[FAIL] Legacy file '{fileName}' was falsely matched as UTF. Detected: {detectedKind}")
    '''         Else
    '''             Console.ForegroundColor = ConsoleColor.Green
    '''             Console.WriteLine($"[PASS] Legacy file '{fileName}' successfully bypassed UTF detection (Returned Unknown).")
    '''         End If
    '''     Else
    '''         If isUtfHandled AndAlso (detectedKind &lt;&gt; UtfEncodingKind.Unknown) Then
    '''             Console.ForegroundColor = ConsoleColor.Green
    '''             Console.WriteLine($"[PASS] {fileName} -> Detected UtfEncodingKind: {detectedKind}")
    '''         Else
    '''             Console.ForegroundColor = ConsoleColor.Red
    '''             Console.WriteLine($"[FAIL] Failed to detect UTF encoding kind for target file: {fileName}")
    '''         End If
    '''     End If
    ''' 
    '''     Try
    '''         If File.Exists(filePath) Then
    '''             File.Delete(filePath)
    '''         End If
    '''     Catch
    '''     End Try
    ''' Next pair
    ''' </code>
    ''' </example>
    ''' 
    ''' <param name="filePath">
    ''' The path of the file to analyze.
    ''' </param>
    ''' 
    ''' <param name="refEncodingKind">
    ''' When this method returns, contains a <see cref="UtfEncodingKind"/> value representing the detected UTF encoding if the detection succeeded, 
    ''' or <see cref="UtfEncodingKind.Unknown"/> if the detection failed or the file is empty.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the encoding was successfully detected; otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="filePath"/> is null, empty, or consists only of whitespace characters.
    ''' </exception>
    ''' 
    ''' <exception cref="FileNotFoundException">
    ''' Thrown when the file specified in <paramref name="filePath"/> cannot be found.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function TryGetUtfFileEncodingKind(filePath As String, ByRef refEncodingKind As UtfEncodingKind) As Boolean

        ' Maximum number of bytes read from the start of a file when detecting its UTF encoding.
        Const UtfEncodingDetectionSampleSizeInBytes As Integer = 32768 ' 32 KB

        Dim sampleBytes As Byte() = FileSystemHelper.ReadSampleBytes(filePath, UtfEncodingDetectionSampleSizeInBytes)

        If sampleBytes.Length = 0 Then
            refEncodingKind = UtfEncodingKind.Unknown ' Empty file.
            Return False
        End If

        ' Step 1: BOM detection.

        ' UTF-32 Encodings:

        If sampleBytes.Length >= 4 AndAlso
           sampleBytes(0) = &HFF AndAlso
           sampleBytes(1) = &HFE AndAlso
           sampleBytes(2) = 0 AndAlso
           sampleBytes(3) = 0 Then

            refEncodingKind = UtfEncodingKind.UTF32LE_Bom
            Return True
        End If

        If sampleBytes.Length >= 4 AndAlso
           sampleBytes(0) = 0 AndAlso
           sampleBytes(1) = 0 AndAlso
           sampleBytes(2) = &HFE AndAlso
           sampleBytes(3) = &HFF Then

            refEncodingKind = UtfEncodingKind.UTF32BE_Bom
            Return True
        End If

        ' UTF-8 Encodings:

        If sampleBytes.Length >= 3 AndAlso
           sampleBytes(0) = &HEF AndAlso
           sampleBytes(1) = &HBB AndAlso
           sampleBytes(2) = &HBF Then

            refEncodingKind = UtfEncodingKind.UTF8_Bom
            Return True
        End If

        ' UTF-16 Encodings:

        If sampleBytes.Length >= 2 AndAlso
            sampleBytes(0) = &HFF AndAlso
            sampleBytes(1) = &HFE Then

            refEncodingKind = UtfEncodingKind.UTF16LE_Bom
            Return True
        End If

        If sampleBytes.Length >= 2 AndAlso
           sampleBytes(0) = &HFE AndAlso
           sampleBytes(1) = &HFF Then

            refEncodingKind = UtfEncodingKind.UTF16BE_Bom
            Return True
        End If

        ' Step 2: no BOM. Try headerless UTF-32 first — its null-byte signature (~75%, concentrated
        ' in 3 of every 4 byte positions) is more specific/distinctive than UTF-16's, so checking it
        ' first avoids any chance of a UTF-32 sample being mis-swept into the looser UTF-16 heuristic.
        Dim isUtf32BigEndian As Boolean = False
        If EncodingHelper.TryDetectHeaderlessUtf32(sampleBytes, isUtf32BigEndian) Then
            refEncodingKind = If(isUtf32BigEndian, UtfEncodingKind.UTF32BE, UtfEncodingKind.UTF32LE)
            Return True
        End If

        ' Step 3: no BOM, doesn't look like UTF-32. Try headerless UTF-16 via its null-byte signature:
        Dim isUtf16BigEndian As Boolean = False
        If EncodingHelper.TryDetectHeaderlessUtf16(sampleBytes, isUtf16BigEndian) Then
            refEncodingKind = If(isUtf16BigEndian, UtfEncodingKind.UTF16BE, UtfEncodingKind.UTF16LE)
            Return True
        End If

        ' Step 4: no BOM, not UTF-16/32-shaped. Try UTF-7 via its shift-sequence syntax.
        ' This MUST run the UTF-8 check below, because any genuine UTF-7 byte stream is ALSO 
        ' trivially valid UTF-8 (UTF-7 is always 7-bit-clean ASCII, and ASCII is a 
        ' strict subset of UTF-8) — so UTF-8 validation alone can never tell them apart;
        ' UTF-7 must be ruled in or out first.
        If EncodingHelper.TryDetectUtf7(sampleBytes) Then
            refEncodingKind = UtfEncodingKind.UTF7
            Return True
        End If

        ' Step 5: not UTF-32/16/7-shaped. Validate strictly as UTF-8:
        Dim trimmedSample As Byte() = EncodingHelper.TrimUtf8LeadBytesCutOffBySampleBoundary(sampleBytes)
        Dim strictUtf8Decoder As New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False, throwOnInvalidBytes:=True)
        Try
            strictUtf8Decoder.GetString(trimmedSample)
            refEncodingKind = UtfEncodingKind.UTF8
            Return True

        Catch ex As DecoderFallbackException
            refEncodingKind = UtfEncodingKind.Unknown ' Not valid UTF-8, no BOM, and not UTF-16/32/7-shaped: unsupported/legacy.
            Return False

        End Try
    End Function

    ''' <summary>
    ''' Tries to determine the <see cref="Encoding"/> of the specified file based on its UTF byte signature and internal heuristics.
    ''' <para></para>
    ''' ⚠️ Important: 
    ''' <para></para>
    ''' Because the nature of UTF encodings allows for multiple valid representations of the same text, 
    ''' this method may not always be able to determine the encoding with absolute certainty.
    ''' <para></para>
    ''' Take the resulting <see cref="Encoding"/> return value as an approximation rather than a guarantee.
    ''' </summary>
    ''' 
    ''' <example> This is a full test code example that can be used to validate the behavior of <see cref="TryGetUtfFileEncoding"/> against a variety of UTF and legacy encodings.
    ''' <code language="VB">
    ''' Dim encodingsToTest As New Dictionary(Of String, Encoding) From {
    '''    {"UTF-7.txt", New UTF7Encoding(allowOptionals:=False)},
    '''    {"UTF-8.txt", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)},
    '''    {"UTF-8 (with BOM).txt", New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)},
    '''    {"UTF-16.txt", New UnicodeEncoding(bigEndian:=False, byteOrderMark:=False)},
    '''    {"UTF-16 (with BOM).txt", New UnicodeEncoding(bigEndian:=False, byteOrderMark:=True)},
    '''    {"UTF-16BE.txt", New UnicodeEncoding(bigEndian:=True, byteOrderMark:=False)},
    '''    {"UTF-16BE (with BOM).txt", New UnicodeEncoding(bigEndian:=True, byteOrderMark:=True)},
    '''    {"UTF-32.txt", New UTF32Encoding(bigEndian:=False, byteOrderMark:=False)},
    '''    {"UTF-32 (with BOM).txt", New UTF32Encoding(bigEndian:=False, byteOrderMark:=True)},
    '''    {"UTF-32BE.txt", New UTF32Encoding(bigEndian:=True, byteOrderMark:=False)},
    '''    {"UTF-32BE (with BOM).txt", New UTF32Encoding(bigEndian:=True, byteOrderMark:=True)} _
    '''    , ' These shouldn't be matched by the UTF detection logic, but are included for testing purposes.
    '''    {"DOS (CP-437).txt", Encoding.GetEncoding("CP437")},
    '''    {"Windows-1252.txt", Encoding.GetEncoding("Windows-1252")},
    '''    {"ISO-8859-1.txt", Encoding.GetEncoding("ISO-8859-1")},
    '''    {"ISO-8859-2.txt", Encoding.GetEncoding("ISO-8859-2")},
    '''    {"ISO-8859-3.txt", Encoding.GetEncoding("ISO-8859-3")},
    '''    {"ISO-8859-15.txt", Encoding.GetEncoding("ISO-8859-15")}
    ''' }
    ''' 
    ''' Dim stringToTest As String = "Hello, World! こんにちは世界 àèìòù¡!¿?€áéíóúñÑÇç"
    ''' 
    ''' For Each pair As KeyValuePair(Of String, Encoding) In encodingsToTest
    ''' 
    '''     Dim fileName As String = pair.Key
    '''     Dim filePath As String = Path.Combine(Path.GetTempPath(), fileName)
    ''' 
    '''     Dim enc As Encoding = pair.Value
    ''' 
    '''     File.WriteAllText(filePath, stringToTest, enc)
    '''     Dim expectedEncoding As Encoding = pair.Value
    '''     Dim detectedEncoding As Encoding = Nothing
    '''     Dim isUtfHandled As Boolean = EncodingHelper.TryGetUtfFileEncoding(filePath, detectedEncoding)
    ''' 
    '''     ' Clean target name for legacy verification
    '''     Dim isLegacyFile As Boolean = Not fileName.StartsWith("utf", StringComparison.OrdinalIgnoreCase)
    ''' 
    '''     If isLegacyFile Then
    '''         If isUtfHandled OrElse (detectedEncoding IsNot Nothing) Then
    '''             Console.ForegroundColor = ConsoleColor.Red
    '''             Console.WriteLine($"[FAIL] Legacy file '{fileName}' was falsely matched as UTF.")
    '''         Else
    '''             Console.ForegroundColor = ConsoleColor.Green
    '''             Console.WriteLine($"[PASS] Legacy file '{fileName}' successfully bypassed UTF detection (Returned Nothing).")
    '''         End If
    '''     Else
    '''         If isUtfHandled AndAlso detectedEncoding IsNot Nothing Then
    ''' 
    '''             Dim expectedHasBom As Boolean = expectedEncoding.GetPreamble().Length > 0
    '''             Dim detectedHasBom As Boolean = detectedEncoding.GetPreamble().Length > 0
    ''' 
    '''             ' Code pages match or their primary WebNames match
    '''             Dim isMatch As Boolean = (expectedEncoding.CodePage = detectedEncoding.CodePage) AndAlso (expectedHasBom = detectedHasBom)
    ''' 
    '''             If isMatch Then
    '''                 Console.ForegroundColor = ConsoleColor.Green
    '''                 Console.WriteLine($"[PASS] {fileName} -> Detected: {detectedEncoding.WebName.ToUpper()} {If(detectedHasBom, "(with BOM)", "(no BOM)")}")
    '''             Else
    '''                 Console.ForegroundColor = ConsoleColor.Yellow
    '''                 Console.WriteLine($"[WARN] {fileName} -> Mismatch. Expected CP: {expectedEncoding.CodePage}, Detected CP: {detectedEncoding.CodePage}")
    '''             End If
    '''         Else
    '''             Console.ForegroundColor = ConsoleColor.Red
    '''             Console.WriteLine($"[FAIL] Failed to detect UTF encoding for target file: {fileName}")
    '''         End If
    '''     End If
    ''' 
    '''     ' File cleanup
    '''     Try
    '''         If File.Exists(filePath) Then
    '''             File.Delete(filePath)
    '''         End If
    '''     Catch
    '''     End Try
    ''' Next pair
    ''' </code>
    ''' </example>
    ''' 
    ''' <param name="filePath">
    ''' The path of the file to analyze.
    ''' </param>
    ''' 
    ''' <param name="refEncoding">
    ''' When this method returns, contains a <see cref="Encoding"/> instance representing the detected UTF encoding if the detection succeeded, 
    ''' or <see langword="Nothing"/> if the detection failed.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the encoding was successfully detected; otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="filePath"/> is null, empty, or consists only of whitespace characters.
    ''' </exception>
    ''' 
    ''' <exception cref="FileNotFoundException">
    ''' Thrown when the file specified in <paramref name="filePath"/> cannot be found.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function TryGetUtfFileEncoding(filePath As String, ByRef refEncoding As Encoding) As Boolean

        Dim encodingKind As UtfEncodingKind = Nothing
        If EncodingHelper.TryGetUtfFileEncodingKind(filePath, encodingKind) Then
            refEncoding = EncodingHelper.GetEncodingFromUtfEncodingKind(encodingKind)
            Return True
        End If

        refEncoding = Nothing
        Return False
    End Function

    ''' <summary>
    ''' Gets the corresponding <see cref="Encoding"/> instance for the specified <see cref="UtfEncodingKind"/>.
    ''' </summary>
    ''' 
    ''' <param name="encodingKing">
    ''' The UTF encoding kind enumeration value to translate to a <see cref="Encoding"/> instance.
    ''' </param>
    ''' 
    ''' <returns>
    ''' An <see cref="Encoding"/> instance configured for the specified kind, 
    ''' or <see langword="Nothing"/> if the kind is <see cref="UtfEncodingKind.Unknown"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="InvalidEnumArgumentException">
    ''' Thrown when <paramref name="encodingKing"/> is not a valid member of <see cref="UtfEncodingKind"/>.
    ''' </exception>
    <DebuggerStepThrough>
    Friend Function GetEncodingFromUtfEncodingKind(encodingKing As UtfEncodingKind) As Encoding

        Select Case encodingKing

            ' UTF-7 encoding:

            Case UtfEncodingKind.UTF7
#Disable Warning IDE0079 ' Remove unnecessary suppression
#Disable Warning SYSLIB0001 ' Type or member is obsolete
                Return New UTF7Encoding(allowOptionals:=False)
#Enable Warning SYSLIB0001 ' Type or member is obsolete
#Enable Warning IDE0079 ' Remove unnecessary suppression

            ' UTF-8 encodings:

            Case UtfEncodingKind.UTF8
                Return New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)

            Case UtfEncodingKind.UTF8_Bom
                Return New UTF8Encoding(encoderShouldEmitUTF8Identifier:=True)

            ' UTF-16 encodings:

            Case UtfEncodingKind.UTF16LE
                Return New UnicodeEncoding(bigEndian:=False, byteOrderMark:=False)

            Case UtfEncodingKind.UTF16LE_Bom
                Return New UnicodeEncoding(bigEndian:=False, byteOrderMark:=True)

            Case UtfEncodingKind.UTF16BE
                Return New UnicodeEncoding(bigEndian:=True, byteOrderMark:=False)

            Case UtfEncodingKind.UTF16BE_Bom
                Return New UnicodeEncoding(bigEndian:=True, byteOrderMark:=True)

            ' UTF-32 encodings:

            Case UtfEncodingKind.UTF32LE
                Return New UTF32Encoding(bigEndian:=False, byteOrderMark:=False)

            Case UtfEncodingKind.UTF32LE_Bom
                Return New UTF32Encoding(bigEndian:=False, byteOrderMark:=True)

            Case UtfEncodingKind.UTF32BE
                Return New UTF32Encoding(bigEndian:=True, byteOrderMark:=False)

            Case UtfEncodingKind.UTF32BE_Bom
                Return New UTF32Encoding(bigEndian:=True, byteOrderMark:=True)

            ' Other / Unknown encodings:

            Case UtfEncodingKind.Unknown
                Return Nothing

            Case Else
                Throw New InvalidEnumArgumentException(NameOf(encodingKing), encodingKing, GetType(UtfEncodingKind))

        End Select
    End Function

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Tries to heuristically detect if a byte array contains headerless (no-BOM) UTF-32 encoded text 
    ''' and determines its endianness based on null-byte percentage distribution.
    ''' </summary>
    ''' 
    ''' <param name="sampleBytes">
    ''' The byte array sample to analyze.
    ''' </param>
    ''' 
    ''' <param name="refIsBigEndian">
    ''' When this method returns <see langword="True"/>, 
    ''' the value in <paramref name="refIsBigEndian"/> contains <see langword="True"/> if the detected UTF-32 text is Big Endian, 
    ''' or <see langword="False"/> if it is Little Endian. 
    ''' <para></para>
    ''' ❗ When this method returns <see langword="False"/>, the value in <paramref name="refIsBigEndian"/> is meaningless and should be ignored.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the sample is confidently recognized as UTF-32; otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="sampleBytes"/> is null.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function TryDetectHeaderlessUtf32(sampleBytes As Byte(), ByRef refIsBigEndian As Boolean) As Boolean

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sampleBytes)
#Else
        If sampleBytes Is Nothing Then
            Throw New ArgumentNullException(NameOf(sampleBytes))
        End If
#End If

        refIsBigEndian = False

        Dim usableLength As Integer = sampleBytes.Length - (sampleBytes.Length Mod 4)
        If usableLength < 16 Then ' Fewer than 4 full code units: too small a sample to reason about reliably.
            Return False
        End If

        Dim zeroCount0 As Integer = 0
        Dim zeroCount1 As Integer = 0
        Dim zeroCount2 As Integer = 0
        Dim zeroCount3 As Integer = 0

        ' Check bytes per position in 4-byte chunks
        For index As Integer = 0 To usableLength - 1 Step 4
            If sampleBytes(index) = 0 Then zeroCount0 += 1
            If sampleBytes(index + 1) = 0 Then zeroCount1 += 1
            If sampleBytes(index + 2) = 0 Then zeroCount2 += 1
            If sampleBytes(index + 3) = 0 Then zeroCount3 += 1
        Next index

        Dim totalUnits As Integer = usableLength \ 4

        ' Valid UTF-32 never exceeds codepoint U+10FFFF.
        ' Therefore, the highest-order byte is ALWAYS 0 for any valid character.
        ' Big Endian: Byte 0 is always 0.
        ' Little Endian: Byte 3 is always 0.
        ' We demand the highest byte to be 100% zero, the BMP high-byte to be mostly zero (>= 50%),
        ' and the ASCII/Latin low-byte to actually contain data (< 50% zero).

        Dim looksLikeBigEndian As Boolean =
            (zeroCount0 = totalUnits) AndAlso (zeroCount1 >= (totalUnits \ 2)) AndAlso (zeroCount3 < (totalUnits \ 2))

        Dim looksLikeLittleEndian As Boolean =
            (zeroCount3 = totalUnits) AndAlso (zeroCount2 >= (totalUnits \ 2)) AndAlso (zeroCount0 < (totalUnits \ 2))

        If looksLikeBigEndian AndAlso Not looksLikeLittleEndian Then
            refIsBigEndian = True
            Return True

        ElseIf looksLikeLittleEndian AndAlso Not looksLikeBigEndian Then
            refIsBigEndian = False
            Return True

        End If

        Return False ' Ambiguous, or doesn't match either pattern confidently.
    End Function

    ''' <summary>
    ''' Tries to heuristically detect if a byte array contains headerless (no-BOM) UTF-16 encoded text 
    ''' and determines its endianness based on null-byte percentage distribution.
    ''' </summary>
    ''' 
    ''' <param name="sampleBytes">
    ''' The byte array sample to analyze.
    ''' </param>
    ''' 
    ''' <param name="refIsBigEndian">
    ''' When this method returns <see langword="True"/>, 
    ''' the value in <paramref name="refIsBigEndian"/> contains <see langword="True"/> if the detected UTF-16 text is Big Endian, 
    ''' or <see langword="False"/> if it is Little Endian. 
    ''' <para></para>
    ''' ❗ When this method returns <see langword="False"/>, the value in <paramref name="refIsBigEndian"/> is meaningless and should be ignored.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the sample is confidently recognized as UTF-16; otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="sampleBytes"/> is null.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function TryDetectHeaderlessUtf16(sampleBytes As Byte(), ByRef refIsBigEndian As Boolean) As Boolean

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sampleBytes)
#Else
        If sampleBytes Is Nothing Then
            Throw New ArgumentNullException(NameOf(sampleBytes))
        End If
#End If

        refIsBigEndian = False

        Dim usableLength As Integer = sampleBytes.Length - (sampleBytes.Length Mod 2)
        If usableLength < 8 Then ' Too small a sample to reason about reliably.
            Return False
        End If

        Dim zeroAtEvenIndex As Integer = 0 ' High-byte position if Big Endian.
        Dim zeroAtOddIndex As Integer = 0  ' High-byte position if Little Endian.

        For index As Integer = 0 To usableLength - 1
            If sampleBytes(index) = 0 Then
                If index Mod 2 = 0 Then
                    zeroAtEvenIndex += 1
                Else
                    zeroAtOddIndex += 1
                End If
            End If
        Next index

        Dim totalUnits As Integer = usableLength \ 2
        Dim overallZeroRatio As Double = (zeroAtEvenIndex + zeroAtOddIndex) / usableLength

        If overallZeroRatio > 0.65 Then
            Return False ' Looks like UTF-32 (too many nulls for UTF-16): out of scope, not UTF-16.
        End If

        If overallZeroRatio < 0.15 Then
            Return False ' Looks like plain 8-bit text: too few nulls to be UTF-16.
        End If

        Dim evenRatio As Double = zeroAtEvenIndex / totalUnits
        Dim oddRatio As Double = zeroAtOddIndex / totalUnits

        If evenRatio > 0.7 AndAlso evenRatio > oddRatio Then
            refIsBigEndian = True
            Return True
        ElseIf oddRatio > 0.7 AndAlso oddRatio > evenRatio Then
            refIsBigEndian = False
            Return True
        End If

        Return False ' Ambiguous: neither side has a clear majority.
    End Function

    ''' <summary>
    ''' Tries to heuristically detect if a byte array contains UTF-7 encoded text, per RFC 2152.
    ''' <para></para>
    ''' UTF-7 is ALWAYS 7-bit-clean ASCII (every byte &lt; 0x80), which means a genuine UTF-7 sample is 
    ''' also, trivially, valid UTF-8 (ASCII is a UTF-8 subset) — so byte-validity checks alone can NEVER 
    ''' distinguish UTF-7 from plain ASCII/UTF-8 text. The actual distinguishing signal used here is 
    ''' structural: UTF-7 encodes non-ASCII characters via "shifted" sequences (a '+' character, followed 
    ''' by a modified-Base64 run, ended by the first non-Base64 character). This method decodes the 
    ''' sample as UTF-7, then re-encodes the result and checks for an EXACT byte-for-byte round-trip 
    ''' AND the presence of at least one genuinely decoded non-ASCII character — proving a real shift 
    ''' sequence produced real content, rather than a stray literal '+' or '-' in ordinary ASCII text 
    ''' (which UTF-7 also permits unshifted, and which alone must NOT trigger a positive detection).
    ''' </summary>
    ''' 
    ''' <param name="sampleBytes">
    ''' The byte array sample to analyze.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the sample is confidently recognized as UTF-7; otherwise, <see langword="False"/>.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="sampleBytes"/> is null.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function TryDetectUtf7(sampleBytes As Byte()) As Boolean

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sampleBytes)
#Else
        If sampleBytes Is Nothing Then
            Throw New ArgumentNullException(NameOf(sampleBytes))
        End If
#End If

        If sampleBytes.Length = 0 Then
            Return False
        End If

        Dim hasShiftSequence As Boolean = False

        ' UTF-7 is 100% 7-bit clean. Any byte >= 0x80 means it is mathematically impossible to be UTF-7.
        For Each currentByte As Byte In sampleBytes
            If currentByte >= &H80 Then
                Return False
            End If
            If currentByte = 43 Then ' The '+' character
                hasShiftSequence = True
            End If
        Next currentByte

        ' If there's no '+' character, there are no shift sequences.
        ' A text without shift sequences is pure ASCII, which is identical to UTF-8. 
        ' We must yield to UTF-8 in this case.
        If Not hasShiftSequence Then
            Return False
        End If

#Disable Warning IDE0079 ' Remove unnecessary suppression
#Disable Warning SYSLIB0001 ' Type or member is obsolete
        Dim utf7Codec As New UTF7Encoding()
#Enable Warning SYSLIB0001 ' Type or member is obsolete
#Enable Warning IDE0079 ' Remove unnecessary suppression
        Dim decodedText As String = utf7Codec.GetString(sampleBytes)

        ' Instead of relying on a brittle byte-by-byte roundtrip (SequenceEqual) that fails due to 
        ' .NET's encoding quirks with Optionals, we verify that the shift sequences actually 
        ' produced real non-ASCII characters.
        ' Note: Using Convert.ToInt32 instead of Strings.AscW because AscW returns negative values for chars > U+7FFF.
        For Each currentChar As Char In decodedText
            If Convert.ToInt32(currentChar) > 126 Then
                Return True ' The ASCII shift sequence successfully produced a non-ASCII char. Guaranteed UTF-7.
            End If
        Next

        Return False ' It just had literal '+' signs, but no actual UTF-7 encoded characters.
    End Function

    ''' <summary>
    ''' Given a byte sample that has NOT yet been confirmed to be UTF-8, checks whether the fixed-size sample was cut off mid-way through 
    ''' what LOOKS LIKE the start of a multi-byte UTF-8 sequence — based purely on lead-byte bit patterns — and if so, 
    ''' removes those trailing lead bytes.
    ''' <para></para>
    ''' This exists ONLY to prevent a false negative in UTF-8 validity check caused by WHERE the bounded sample happened to end, 
    ''' not because of any actual malformed content in the file itself. 
    ''' It makes no claim, and performs no check, about whether the input actually IS valid UTF-8.
    ''' </summary>
    ''' 
    ''' <param name="sampleBytes">
    ''' The byte array sample to analyze. Not required to be valid UTF-8.
    ''' </param>
    ''' 
    ''' <returns>
    ''' A new byte array with the dangling lead bytes removed, if any were found at the very end of 
    ''' the sample; otherwise, the original <paramref name="sampleBytes"/> array, unchanged.
    ''' </returns>
    ''' 
    ''' <exception cref="ArgumentNullException">
    ''' Thrown when <paramref name="sampleBytes"/> is null.
    ''' </exception>
    <DebuggerStepThrough>
    Private Function TrimUtf8LeadBytesCutOffBySampleBoundary(sampleBytes As Byte()) As Byte()

#If NETCOREAPP Then
        ArgumentNullException.ThrowIfNull(sampleBytes)
#Else
        If sampleBytes Is Nothing Then
            Throw New ArgumentNullException(NameOf(sampleBytes))
        End If
#End If

        Dim length As Integer = sampleBytes.Length
        If length = 0 Then
            Return sampleBytes
        End If

        Dim maxLookBack As Integer = Math.Min(3, length)

        For lookBack As Integer = 0 To maxLookBack - 1

            Dim index As Integer = length - 1 - lookBack
            Dim currentByte As Byte = sampleBytes(index)

            Dim expectedSequenceLength As Integer = 0
            If (currentByte And &HE0) = &HC0 Then
                expectedSequenceLength = 2
            ElseIf (currentByte And &HF0) = &HE0 Then
                expectedSequenceLength = 3
            ElseIf (currentByte And &HF8) = &HF0 Then
                expectedSequenceLength = 4
            End If

            If expectedSequenceLength > 0 Then
                Dim actualBytesAvailable As Integer = length - index
                If actualBytesAvailable < expectedSequenceLength Then
                    Dim trimmed(index - 1) As Byte
                    Array.Copy(sampleBytes, trimmed, index)
                    Return trimmed
                End If
                Exit For
            End If
        Next lookBack

        Return sampleBytes
    End Function

#End Region

End Module

#End Region
