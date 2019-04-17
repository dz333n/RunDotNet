using System;

namespace TestLibraryAssembly
{
    public class ClassNumberOne
    {
        public static bool ArgumentMoreThanFour(string[] args)
            => args.Length > 4;
    }

    public class ClassTwo
    {
        public static void DisplaySomethingOnScreen()
            => Console.WriteLine("Hello!");
    }
}
