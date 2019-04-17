using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RunDotNet
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                PrintHelp();
                return;
            }

            ConLine("OS: " + Environment.OSVersion.ToString());
            ConLine(".NET: " + Environment.Version.ToString());
            var asmf = new FileInfo(args[0]);

            if (!asmf.Exists)
            {
                ConLine($"Assembly '{asmf.FullName}' not found.");
                return;
            }
            
            Directory.SetCurrentDirectory(asmf.Directory.FullName);
            ConLine($"Current directory: {Directory.GetCurrentDirectory()}");

            Assembly asm;

            try
            {
                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
                asm = Assembly.LoadFile(asmf.FullName);
                // ConLine($"Assembly loaded: {asm}");
            }
            catch (Exception ex)
            {
                ConLine($"Can't load assembly '{asmf.FullName}'");
                ConLine(ex.ToString());
                return;
            }

            MethodInfo EntryPoint = asm.EntryPoint;
            List<string> ProgramArgs = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg.ToLower() == "/e")
                {
                    i++; // next argument (entry point name)
                    string search = args[i];
                    EntryPoint = SearchEntryPoint(asm, search);
                    continue; // next argument (arguments)
                }
                else if(arg.ToLower() == "/s")
                {
                    PrintMethod(EntryPoint);
                    return;
                }

                ProgramArgs.Add(arg);
            }

            if (EntryPoint == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                ConLine("Entry point not found.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            else PrintMethod(EntryPoint);

            object[] margs = new object[] { };

        
            var p = EntryPoint.GetParameters();

            if (p.Length >= 1 && p[0].ParameterType.Name == typeof(string[]).Name)
            {
                ConLine($"Putting arguments in parameter '{p[0].Name}'.");
                margs = new object[] { ProgramArgs.ToArray() };
            }

            try
            {
                ConLine("Invoke method");

                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                var obj = EntryPoint.Invoke(null, margs);

                Console.ForegroundColor = ConsoleColor.Green;
                ConLine("Function invoked. Returned: " + obj != null ? obj : "null");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (TargetInvocationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                ConLine("Invocation exception:");
                ConLine(ex.InnerException.GetType().Name);
                Console.ForegroundColor = ConsoleColor.White;
                ConLine(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                ConLine(ex.GetType().Name);
                Console.ForegroundColor = ConsoleColor.White;
                ConLine(ex.ToString());
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            ConLine("Finished");
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var file = SearchForAssembly(GetDllNameFromFull(args.Name));

            if (file != null)
                return Assembly.LoadFile(file.FullName);
            else
                return null;
        }

        private static FileInfo SearchForAssembly(string name)
        {
            List<string> folders = new List<string>() { Directory.GetCurrentDirectory(), GetAssemblyGAC_MSIL(name) };

            foreach (var folder in folders)
                if(Directory.Exists(folder))
                    if (File.Exists(Path.Combine(folder, name)))
                        return new FileInfo(Path.Combine(folder, name));

            return null;
        }

        private static string GetAssemblyGAC_MSIL(string name)
        {
            var namenodll = name.EndsWith(".dll") ? name.Remove(name.Length - 4, 4) : name;
            var gmpath = new DirectoryInfo(Path.Combine(Path.Combine(Path.Combine(new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.System)).Parent.FullName, "Microsoft.NET"), "assembly"), namenodll));

            if (gmpath.Exists)
            {
                var files = gmpath.GetFiles(name, SearchOption.AllDirectories);

                foreach (var file in files)
                    return file.Directory.FullName;
            }

            return "";
        }

        private static string GetDllNameFromFull(string full)
            => full.Split(',')[0] + ".dll"; // hack? 

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            ConLine("+ Assembly " + args.LoadedAssembly);
        }

        private static Module Asm_ModuleResolve(object sender, ResolveEventArgs e)
        {
            ConLine(sender.GetType().FullName);
            var asm = sender as Assembly;
            ConLine($"[{asm.GetName().Name}] {e.Name}");
            return null;
        }

        static void ConLine(object line) => Console.WriteLine("[run.net] " + line);

        static void PrintMethod(MethodInfo m)
        {
            if (m == null)
            {
                ConLine("Method not found.");
                return;
            }

            var old = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(m.ReturnType.Name + " ");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(m.DeclaringType.Namespace + ".");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(m.Name + "(");

            var p = m.GetParameters();
            for (int x = 0; x < p.Length; x++)
            {
                var param = p[x];

                if (x != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write(", ");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"{param.ParameterType.Name} ");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(param.Name);

                //try // hack for old .net instead of using HasDefaultValue
                //{
                //    string value = param.DefaultValue != null ? param.DefaultValue.ToString() : "null";

                //    Console.ForegroundColor = ConsoleColor.Gray;
                //    Console.Write(" = ");

                //    Console.ForegroundColor = ConsoleColor.White;

                //    if (param.DefaultValue is string)
                //    {
                //        Console.ForegroundColor = ConsoleColor.Yellow;
                //        Console.Write($"\"{value}\"");
                //    }
                //    else Console.Write(value);
                //}
                //catch { }
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(m.Name + ");");

            Console.ForegroundColor = old;
        }

        static MethodInfo SearchEntryPoint(Assembly asm, string text)
        {
            return null;
        }

        static void PrintHelp()
        {
            Console.WriteLine("RunDotNet [\"assembly path\"] [/e \"entry point\"] [/s] [arguments]");
            Console.WriteLine();
            Console.WriteLine("    /e \"entry point\" (optional) - search and invoke defined entry point");
            Console.WriteLine("    /s (optional) - do not invoke entry point, just show it");
            Console.WriteLine("    arguments (optional) - include arguments for entry point");
        }
    }
}
