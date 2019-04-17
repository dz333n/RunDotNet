using System;
using System.Threading;

namespace TestProgramAssembly
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Console.Write($"Arguments ({args.Length}): ");
            foreach (var arg in args)
                Console.Write("\"" + arg + "\" ");
            Console.WriteLine();

            Console.WriteLine("Sleeping 2 seconds");
            Thread.Sleep(2000);

            Console.WriteLine("Returning code 0");
            return 0;
        }
    }
}
