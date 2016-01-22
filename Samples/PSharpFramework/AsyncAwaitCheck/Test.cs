using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Tooling;
using System.Reflection;

namespace AsyncAwaitCheck
{
    public class Test
    {
        static void Main(string[] args)
        {
            Test.Execute();
            Console.WriteLine("Done executing test");
            Console.ReadLine();
        }

        [Microsoft.PSharp.Test]
        public static void Execute()
        {
            PSharpRuntime.CreateMachine(typeof(TaskCreator));
        }
    }
}
