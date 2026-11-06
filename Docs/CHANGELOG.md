# CS-VB Line Endings Normalizer Change Log 📋

## v1.1 *(current)* 🆕
- Fixed a critical bug that could corrupt files saved in legacy encodings (such as Windows-1252 or ISO-8859-1): 
  when a file contained accented characters near its end, it could be wrongly detected as UTF-8 and rewritten, replacing those characters with "�". 
  The whole file is now validated, and any file that is not valid UTF-8, UTF-16 or UTF-32 is skipped and left untouched.
- Fixed the exit code shown in the final message of the .NET 10 build, which was printed as decimal digits prefixed with "0x" instead of a real hexadecimal value.

## v1.0
Initial Release.