using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MakeSbx;

class Program
{
    static void Main(string[] args)
    {
        string outputPath = "";
        string currentDir = Directory.GetCurrentDirectory();
        string? inputPath = currentDir;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "i")
            {
                inputPath = args[++i];
            }
            else if (arg == "o")
            {
                outputPath = args[++i];
            }
            else if (arg == "h" || arg == "--help")
            {
                Console.WriteLine("Usage: MakeSbx [i <inputPath>] [o <outputPath>]");
                Console.WriteLine("  i <inputPath>   Path to the input directory");
                Console.WriteLine("  o <outputPath>  Path to the output file");
                return;
            }
        }
        
        if (string.IsNullOrEmpty(inputPath))
        {
            Console.WriteLine("Input path is required.");
            return;
        }
        
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = Path.Combine(inputPath, Path.GetFileName(inputPath) + ".sbx");
        }
        
        if (!Directory.Exists(inputPath))
        {
            Console.WriteLine($"Input path '{inputPath}' does not exist.");
            return;
        }
        
        if (File.Exists(outputPath))
        {
            Console.WriteLine($"Output file '{outputPath}' already exists. Overwrite? (y/n)");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
            {
                return;
            }
        }
        
        var exePath = typeof(Program).Assembly.Location;
        var exeDir = Path.GetDirectoryName(exePath);
        if (exeDir == null)
        {
            Console.WriteLine("Could not determine the directory of the executable.");
            return;
        }
        var localHaxePath = exeDir + Path.DirectorySeparatorChar + "haxe";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            localHaxePath = exeDir + Path.DirectorySeparatorChar + "haxe.exe";

        ProcessStartInfo startInfo = new ProcessStartInfo(localHaxePath);

        if (!File.Exists(localHaxePath))
        {
            startInfo = new ProcessStartInfo("haxe");
        }
        
        startInfo.WorkingDirectory = inputPath;
        
        startInfo.Arguments = "build.hxml";
        
        Process.Start(startInfo);
    }
}