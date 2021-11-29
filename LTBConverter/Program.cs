using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace LTBConverter
{
    static class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("LTBConverter by Banz99" );
            Console.WriteLine("Tool for unpacking and repacking text inside WF Engine (.ltb) textfiles");
            Console.WriteLine("Usage: LTBConverter.exe [flags] inputfile [outputfile]");
            Console.WriteLine("Supported flags:");
            Console.WriteLine("-r or --repack:\t\t\t\t treats the input file as a previously exported xml file, to rebuild a compatible .ltb file");
            Console.WriteLine("-e CODEPAGE or --encoding CODEPAGE:\t when replacing CODEPAGE with supported values, lets the program read strings with the respective encoding (https://docs.microsoft.com/it-it/dotnet/api/system.text.encodinginfo.getencoding?view=netframework-4.5)");
            Console.WriteLine("-h or --help:\t\t\t\t Display this help screen");
        }

        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0)
            {
                Console.WriteLine("This application can run purely in console mode when arguments are passed via cmd.");
                Application.Run(new FormMain());
            }
            else
            {
                bool repack = false;
                string inputfile = "";
                string outputfile = "";
                int CodePage = System.Text.Encoding.UTF8.CodePage;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-r" || args[i] == "--repack")
                    {
                        repack = true;
                    }
                    else if (args[i] == "-e" || args[i] == "--encoding")
                    {
                        try
                        {
                            CodePage = Convert.ToInt32(args[i + 1]);
                            i++;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("ERROR: Couldn't parse the encoding CodePage.");
                            Console.WriteLine();
                            PrintUsage();
                            return;
                        }
                    }
                    else if (args[i] == "-h" || args[i] == "--help")
                    {
                        PrintUsage();
                        return;
                    }
                    else
                    {
                        if (File.Exists(args[i]))
                        {
                            if (inputfile == "")
                            {
                                inputfile = args[i];
                            }
                            else outputfile = args[i];
                        }
                        else
                        {
                            Console.WriteLine("ERROR: Command or path invalid.");
                            Console.WriteLine();
                            PrintUsage();
                            return;
                        }
                    }
                }
                if (inputfile == "")
                {
                    Console.WriteLine("ERROR: No input file specified.");
                    Console.WriteLine();
                    PrintUsage();
                    return;
                }
                else if (outputfile == "")
                {
                    outputfile = Path.GetDirectoryName(inputfile)+"\\"+ Path.GetFileNameWithoutExtension(inputfile);
                    if (repack)
                        outputfile += "_rep.ltb";
                    else outputfile += ".xml";
                    Console.WriteLine("WARNING: No output file specified. The program will use \""+outputfile+"\"");
                }
                LTBManagement.EncodingCodePage = CodePage;
                if (repack)
                {
                    try
                    {
                        LTBManagement.LTBRepack(inputfile, outputfile);
                        Console.WriteLine("File repacked successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadKey();
                    }
                }
                else
                {
                    try
                    {
                        LTBManagement.LTBExtract(inputfile, outputfile);
                        Console.WriteLine("File extracted successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.ReadKey();
                    }
                }
            }
        }
    }
}
