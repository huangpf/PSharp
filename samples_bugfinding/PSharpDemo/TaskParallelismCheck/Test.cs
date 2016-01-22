using System;
using System.Collections.Generic;
using Microsoft.PSharp;
using Microsoft.PSharp.Tooling;
using System.Reflection;
using Microsoft.PSharp.DynamicAnalysis;

namespace TaskParallelismCheck
{
    public class Test
    {
        static void Main(string[] args)
        {
            Configuration.RunDynamicAnalysis = true;
            Configuration.SchedulingStrategy = "rand"; // "rand" is default
            Configuration.SchedulingIterations = 10000;
            Configuration.SchedulingBoundingBound = 1;
            // Configuration.FullExploration = true; // continue on error
            Configuration.DepthBound = 10000;

            Configuration.Seed = 9;

            Configuration.Verbose = 1; // default is 1
            Configuration.RedirectOutput = false;


            AnalysisContext.Create(Assembly.GetExecutingAssembly());
            SCTEngine.Setup();
            
            Configuration.Debug.Clear();
            //Configuration.Debug.Add(DebugType.Testing);

            SCTEngine.Run();

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
