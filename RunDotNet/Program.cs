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

            var asmf = new FileInfo(args[0]);

            if (!asmf.Exists)
            {
                Console.WriteLine($"Assembly '{asmf.FullName}' not found.");
                return;
            }
            
            Directory.SetCurrentDirectory(asmf.Directory.FullName);
            Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");

            Assembly asm;

            try
            {
                asm = Assembly.LoadFile(asmf.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't load assembly '{asmf.FullName}'");
                Console.WriteLine(ex.ToString());
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

            if (EntryPoint == null) return;

            object[] margs = new object[] { };

            Console.WriteLine("Invoking that method...");
        
            var p = EntryPoint.GetParameters();

            if (p.Length >= 1 && p[0].ParameterType.Name == typeof(string[]).Name)
            {
                Console.WriteLine($"Putting arguments in parameter '{p[0].Name}'.");
                margs = new object[] { ProgramArgs.ToArray() };
            }

            try
            {
                var obj = EntryPoint.Invoke(null, margs);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Function invoked. Returned: " + obj != null ? obj : "null");
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (TargetInvocationException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invocation exception:");
                Console.WriteLine(ex.InnerException.GetType().Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.GetType().Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(ex.ToString());
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("[RunDotNet] Program finished");
        }

        static void PrintMethod(MethodInfo m)
        {
            if (m == null)
            {
                Console.WriteLine("Method not found.");
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
