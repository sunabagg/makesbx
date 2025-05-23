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
        bool willOverwrite = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "-i")
            {
                inputPath = args[++i];
            }
            else if (arg == "-o")
            {
                outputPath = args[++i];
            }
            else if (arg == "-h" || arg == "--help")
            {
                Console.WriteLine("Usage: MakeSbx [i <inputPath>] [o <outputPath>]");
                Console.WriteLine("  i <inputPath>   Path to the input directory");
                Console.WriteLine("  o <outputPath>  Path to the output file");
                return;
            }
            else if (arg == "-w" || arg == "--overwrite")
            {
                willOverwrite = true;
            }
        }
        
        if (string.IsNullOrEmpty(inputPath))
        {
            Console.WriteLine("Input path is required.");
            return;
        }
        
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine("Output path is required.");
            return;
        }

        if (!outputPath.Contains("/"))
        {
            outputPath = "./" + outputPath;
        }
        
        if (!outputPath.EndsWith(".sbx") && !outputPath.EndsWith(".sbz") && !outputPath.EndsWith(".sbzip"))
        {
           Console.WriteLine("Output path is not valid.");
              return;
        }
        
        if (!Directory.Exists(inputPath))
        {
            Console.WriteLine($"Input path '{inputPath}' does not exist.");
            return;
        }
        
        if (File.Exists(outputPath) && !willOverwrite)
        {
            Console.WriteLine($"Output file '{outputPath}' already exists. Overwrite? (y/n)");
            var response = Console.ReadLine();
            if (response?.ToLower() != "y")
            {
                return;
            }
        }
        
        var exePath = typeof(Program).Assembly.Location;
        var localHaxePath = exePath + Path.DirectorySeparatorChar + "haxe";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            localHaxePath = exePath + Path.DirectorySeparatorChar + "haxe.exe";

        ProcessStartInfo startInfo = new ProcessStartInfo(localHaxePath);

        if (!File.Exists(localHaxePath))
        {
            startInfo = new ProcessStartInfo("haxe");
        }
        
        startInfo.WorkingDirectory = inputPath;
        
        startInfo.Arguments = "build.hxml";
        
        var proc = Process.Start(startInfo);
        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            Console.WriteLine($"Haxe process exited with code {proc.ExitCode}");
            return;
        }
        
        var mainLuaPath = Path.Combine(inputPath, "main.lua");
        if (!File.Exists(mainLuaPath))
        {
            Console.WriteLine($"main.lua file not found in '{inputPath}'");
            return;
        }
        
        Console.WriteLine($"Reading main.lua file '{mainLuaPath}'");
        var mainLua = File.ReadAllText(mainLuaPath);
        
        var assets = GetAllFiles(inputPath + "/assets");
        
        //var binaryPath = Path.Combine(inputPath, "bin");
        
        //var scripts = GetAllFiles(binaryPath);
        
        var zipFile = new System.IO.Compression.ZipArchive(File.Create(outputPath), System.IO.Compression.ZipArchiveMode.Create);
        
        var mainLuaEntry = zipFile.CreateEntry("main.lua", System.IO.Compression.CompressionLevel.Optimal);
        Console.WriteLine($"Writing main.lua to '{outputPath}'");
        using (var stream = mainLuaEntry.Open())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(mainLua);
            File.Delete(mainLuaPath);
            stream.Write(bytes, 0, bytes.Length);
        }
        
        var sourceMapPath = mainLuaPath + ".map";
        if (File.Exists(sourceMapPath))
        {
            var sourceMapEntry = zipFile.CreateEntry("main.lua.map", System.IO.Compression.CompressionLevel.Optimal);
            Console.WriteLine($"Writing main.lua.map to '{outputPath}'");
            using (var stream = sourceMapEntry.Open())
            {
                var bytes = File.ReadAllBytes(sourceMapPath);
                File.Delete(sourceMapPath);
                stream.Write(bytes, 0, bytes.Length);
            }
        }
        
        foreach (var asset in assets)
        {
            var entry = zipFile.CreateEntry(asset.Key.Replace(inputPath + "/assets/", "assets/"), System.IO.Compression.CompressionLevel.Optimal);
            Console.WriteLine($"Writing asset '{asset.Key}' to '{outputPath}'");
            using (var stream = entry.Open())
            {
                stream.Write(asset.Value, 0, asset.Value.Length);
            }
        }

        /*if (scripts.Any())
        {
            foreach (var script in scripts)
            {
                var entry = zipFile.CreateEntry(script.Key.Replace(binaryPath + "/", "/"), System.IO.Compression.CompressionLevel.Optimal);
                Console.WriteLine($"Writing script '{script.Key}' to '{outputPath}'");
                using (var stream = entry.Open())
                {
                    stream.Write(script.Value, 0, script.Value.Length);
                }
            }
        }*/
        
        
        zipFile.Dispose();
        Console.WriteLine($"Created '{outputPath}'");
        Console.WriteLine("Done.");
    }

    public static Dictionary<string, byte[]>
        GetAllFiles(string path)
    {
        if (!Directory.Exists(path))
        {
            Console.WriteLine($"Directory '{path}' does not exist.");
            return new Dictionary<string, byte[]>();
        }
        
        var assets = new Dictionary<string, byte[]>();

        foreach (var file in Directory.GetFiles(path))
        {
            Console.WriteLine($"Reading file '{file}'");
            var bytes = File.ReadAllBytes(file);
            assets.Add(file, bytes);
        }
        
        foreach (var dir in Directory.GetDirectories(path))
        {
            var dirName = Path.GetFileName(dir);
            var dirAssets = GetAllFiles(dir);
            foreach (var asset in dirAssets)
            {
                assets.Add(asset.Key, asset.Value);
            }
        }
        
        return assets;
    }
}