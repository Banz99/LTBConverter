# LTBConverter
The purpose of this tool is to correctly parse the binary packed file format .ltb created for titles powered by the [WayForward Engine](https://www.pcgamingwiki.com/wiki/Engine:WF_Engine) containing most of these games text, produce an human readable and editable xml file and recreate the .ltb so it can be reimported. Since this actually uses and understands the vast majority of the values inside the file, all the limitations that are usually in place when using an hex editor are virtually gone. In this first release only Little Endian files are supported.

## Features:
1. Hybrid app, allowing you to use its Windows Forms interface or just via console commands when passing at least one parameter.
2. No string size replacement limit.
3. Different string encodings options (Windows 1252 and UTF-8 via GUI, manual CodePage choice in Console mode).
4. 1:1 File recreation when nothing is changed inside the .xml.

## Console Usage:
LTBConverter.exe [flags] inputfile [outputfile]

### [flags]
-r or --repack: Treats the input file as a previously exported xml file, to rebuild a compatible .ltb file.

-e CODEPAGE or --encoding CODEPAGE: Forces a certain string encoding based on its CodePage. [See here for .Net Framework 4.5](https://docs.microsoft.com/en-us/dotnet/api/system.text.encodinginfo.getencoding?view=netframework-4.5)

-h or --help: Displays an help screen similar to this.
