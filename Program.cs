using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestoreSolidityDB
{
    internal class Program
    {
        private static string startPath, resultPath;
        static void Main(string[] args)
        {
            Console.WriteLine("Set start path:");
            startPath = Console.ReadLine();
            Console.WriteLine("Set result path:");
            resultPath = Console.ReadLine();
            Console.WriteLine("Are you ready? Press any button");
            Console.ReadKey();
            Console.Write("Please wait.");
            var num = 0;

            var files = GetAllFiles(startPath).OrderBy(f => new FileInfo(f).LastWriteTime).ToArray();
            var maxDots = Math.Max(10, files.Count() / 100);
            foreach (var file in files)
            {
                if (++num % maxDots == 0) Console.Write(".");
                var datas = ParceContractData(file);
                if (datas == null || datas.Count == 0)
                    continue;

                foreach(var data in datas)
                {
                    var path = Path.Combine(resultPath, data.Key);
                    if (Path.GetExtension(path).ToLower() != ".sol") continue;

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        if (File.Exists(path))
                        {
                            var newPath = NewPath(path);
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                            File.Move(path, newPath);
                        }
                        File.WriteAllText(path, data.Value.ToString(), Encoding.UTF8);
                    }
                    catch (Exception err)
                    {
                        if (!path.Contains("https://"))
                        {
                            Console.WriteLine($"\n{path}\n");
                            Console.WriteLine(err.ToString());
                        }
                    }
                }
            }

            Console.WriteLine("\n\nGood job!");
            Console.WriteLine("Press any button to close");
            Console.ReadKey();
        }

        private static string NewPath(string path, int v = 0)
        {
            if (v == 0) path = path.Replace(resultPath, Path.Combine(resultPath, "_old"));
            var ext = Path.GetExtension(path);
            var rPath = v == 0 ? ext : $"_V{v - 1:00}{ext}";
            var newPath = path.Replace(rPath, $"_V{v:00}{ext}");
            if (File.Exists(newPath))
                return NewPath(newPath, ++v);
            return newPath;
        }

        private static string[] GetAllFiles(string dir)
        {
            var result = new List<string>();
            var files = Directory.GetFiles(dir);
            if (files != null && files.Length > 0)
                result.AddRange(files);

            var subDirs = Directory.GetDirectories(dir);
            foreach (var subDir in subDirs)
            {
                files = GetAllFiles(subDir);
                if (files != null && files.Length > 0)
                    result.AddRange(files);
            }

            return result.ToArray();
        }

        private static Dictionary<string, string> ParceContractData(string filePath)
        {
            try
            {
                var result = new Dictionary<string, string>();

                var text = File.ReadAllText(filePath);
                var startText = "\"sources\":";
                var startIndex = text.IndexOf(startText);
                if (startIndex == -1) return result;
                startIndex += startText.Length;

                var lS = -1;
                var endIndex = startIndex;
                for (var i = startIndex; i < text.Length; i++)
                {
                    if (text[i] == '{') lS += lS == -1 ? 2 : 1;
                    if (text[i] == '}') lS--;
                    if (lS == 0)
                    {
                        endIndex = i + 1;
                        break;
                    }
                }
                text = text.Substring(startIndex, endIndex - startIndex).Trim();
                var obj = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(text);
                var dict = new Dictionary<string, string>();
                foreach (var item in obj)
                {
                    dict[item.Key] = item.Value["content"];
                }
                return dict;
            }
            catch
            {
                Console.WriteLine("Error Json");
                return null;
            }
        }
    }
}
