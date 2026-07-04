<!-- Common Project Tags:
c#
c-sharp 
cli 
cli-tool 
command-line 
command-line-tool 
console-applications 
csharp 
csharp-language 
desktop-app 
desktop-application 
dotnet 
dotnet-core 
dotnetcore 
file-normalizer
line-endings 
netcore 
netframework 
netframework48 
source-code-files 
source-files 
textfile 
tool 
tools 
vbnet 
visual-studio 
visualbasic-language 
visualbasicdotnet 
visualstudio 
vs-code
vscode
windows 
windows-app 
windows-application 
windows-applications 
 -->

<div align="center">
  <img src="/Images/App.ico" width="150" alt="CS-VB_LINES Logo">
  
  <h1>CS-VB Line Endings Normalizer<br>(CS-VB_LINES.exe)</h1>

### Command-line utility to normalize line endings (CRLF / CR / LF)<br>in your C# or VB.NET source-code files.

</div>

------------------

## 👋 Introduction

**CS-VB Line Endings Normalizer** is a command-line interface (CLI) tool designed to ensure line-ending consistency across C# (`*.cs`) and Visual Basic (`*.vb`) source code files. 

The application scans a given directory to process supported source-code files, identifying which files require normalization, and applies the requested canonical line ending style (**CRLF**, **LF**, or **CR**) using a safe, atomic file replacement operation.

## 👌 Features

- Files are read and rewritten preserving their exact text encoding, supporting UTF-8, UTF-16, and UTF-32 in both Little Endian and Big Endian, with or without a Byte Order Mark (BOM).
- Automatically skips empty files, white-space-only files, Visual Studio auto-generated files, and files with unrecognized or unsupported text encodings.
- Updates your source-code files safely by writing changes to a temporary file first, using an atomic replacement with a temporary `.bak` file to ensure data integrity.
- Supports optional recursive scanning (`-r` or `--recurse` flags).
- Provides a safe dry-run environment (`-t` or`--test` flags) to simulate changes without modifying your actual files.
- Uses [Natural sort order](https://en.wikipedia.org/wiki/Natural_sort_order) to process files, making the program execution sequence predictable and easy to follow.

## 🖼️ Screenshots

![screenshot1](/Images/screenshot1.png)

![screenshot2](/Images/screenshot2.png)

## 📝 Requirements

- Microsoft Windows OS.

## 🤖 Getting Started

Download the latest release by clicking [here](https://github.com/ElektroStudios/CS-VB-Line-Endings-Normalizer/releases/latest) and start using it!.

## ⚙️ Usage

```bash
CS-VB_LINES.exe <directory_path> <line_ending_style> [options]
```

| Mandatory&nbsp;Arguments | Description |
| :--- | :--- |
| **<code><directory_path></code>** | The path to the root directory containing the `*.cs` or `*.vb` source-code files to process. |
| **<code><line_ending_style></code>** | The target line ending formatting style to apply. Supported values are: `CRLF`, `CR`, or `LF` (case-insensitive). |

| Options | Description |
| :--- | :--- |
| **<code>&#8209;r</code>**,&nbsp;**<code>&#8209;&#8209;recursive</code>** | Recursively process source-code files in all subdirectories. |
| **<code>&#8209;t</code>**,&nbsp;**<code>&#8209;&#8209;test</code>** | Runs the application in Test Mode (dry-run), simulating the entire process without modifying actual files. |

### 🚀 Examples

Normalize line endings to Windows style (`CRLF`) for files in the the specified root directory:
```bash
VB_Lines_Normalizer.exe "C:\MySolution" crlf
```

Recursively normalize line endings to Unix style (`LF`) for files within the specified root directory and its subdirectories:
```bash
VB_Lines_Normalizer.exe "C:\MySolution" lf -r
```

Run a safe simulation dry-run recursively using Mac style (`CR`) to validate layout changes without modifying actual files:
```bash
VB_Lines_Normalizer.exe "C:\MySolution" cr -r --test
```

---

## 🔄 Change Log

Explore the complete list of changes, bug fixes, and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

This Work has no affiliation, approval or endorsement by the author(s) of the third-party libraries used by this Work.

## 💪 Contributing

Your contribution is highly appreciated!. If you have any ideas, suggestions, or encounter issues, feel free to open an issue by clicking [here](https://github.com/ElektroStudios/CS-VB-Line-Endings-Normalizer/issues/new/choose). 

Your input helps make this Work better for everyone. Thank you for your support! 🚀

## 💰 Beyond Contribution 

This work is distributed for educational purposes and without any profit motive. However, if you find value in my efforts and wish to support and motivate my ongoing work, you may consider contributing financially through the following options:

<br></br>
<p align="center"><img src="/Images/github_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Becoming my sponsor on Github:</h3>
<p align="center">You can show me your support by clicking <a href="https://github.com/sponsors/ElektroStudios/">here</a>, <br align="center">contributing any amount you prefer, and unlocking rewards!</br></p>
<br></br>

<p align="center"><img src="/Images/paypal_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Making a Paypal Donation:</h3>
<p align="center">You can donate to me any amount you like via Paypal by clicking <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=E4RQEV6YF5NZY">here</a>.</p>
<br></br>

<p align="center"><img src="/Images/envato_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Purchasing software of mine at Envato's Codecanyon marketplace:</h3>
<p align="center">If you are a .NET developer, you may want to explore '<b>DevCase Class Library for .NET</b>', <br align="center">a huge set of APIs that I have on sale. Check out the product by clicking <a href="https://codecanyon.net/item/elektrokit-class-library-for-net/19260282">here</a></br><br align="center"><i>It also contains all piece of reusable code that you can find across the source code of my open source works.</i></p>
<br></br>

<h2 align="center"><u>Your support means the world to me! Thank you for considering it!</u> 👍</h2>
