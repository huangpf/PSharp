﻿//-----------------------------------------------------------------------
// <copyright file="Actions1FailTest.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Tests.Unit
{
    [TestClass]
    public class Actions1FailTest : BasePSharpTest
    {
        class Config : Event
        {
            public MachineId Id;
            public Config(MachineId id) : base(-1, -1) { this.Id = id; }
        }

        class E1 : Event
        {
            public E1() : base(1, -1) { }
        }

        class E2 : Event
        {
            public E2() : base(1, -1) { }
        }

        class E3 : Event
        {
            public E3() : base(1, -1) { }
        }

        class E4 : Event
        {
            public E4() : base(1, -1) { }
        }

        class Unit : Event
        {
            public Unit() : base(1, -1) { }
        }

        class Real : Machine
        {
            MachineId GhostMachine;
            bool test = false;

            [Start]
            [OnEntry(nameof(EntryInit))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E4), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            class Init : MachineState { }

            void EntryInit()
            {
                GhostMachine = this.CreateMachine(typeof(Ghost));
                this.Send(GhostMachine, new Config(this.Id));
                this.Send(GhostMachine, new E1());
            }

            void ExitInit()
            {
                test = true;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(Unit), typeof(S2))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Assert(test == true); // holds
                this.Raise(new Unit());
            }

            [OnEntry(nameof(EntryS2))]
            class S2 : MachineState { }

            void EntryS2()
            {
                // this assert is reachable: Real -E1-> Ghost -E2-> Real;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
                this.Assert(false);
            }

            void Action1()
            {
                this.Send(GhostMachine, new E3());
            }
        }

        class Ghost : Machine
        {
            MachineId RealMachine;

            [Start]
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            class Init : MachineState { }

            void Configure()
            {
                RealMachine = (this.ReceivedEvent as Config).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            class S1 : MachineState { }

            void EntryS1()
            {
                this.Send(RealMachine, new E4());
                this.Send(RealMachine, new E2());
            }

            class S2 : MachineState { }
        }

        public static class TestProgram
        {
            [Test]
            public static void Execute(PSharpRuntime runtime)
            {
                runtime.CreateMachine(typeof(Real));
            }
        }
        
        /// <summary>
        /// Tests basic semantics of actions and goto transitions.
        /// </summary>
        [TestMethod]
        public void TestActions1Fail()
        {
            var configuration = Configuration.Create();
            configuration.SuppressTrace = true;
            configuration.Verbose = 2;
            configuration.SchedulingStrategy = SchedulingStrategy.DFS;

            var engine = TestingEngine.Create(configuration, TestProgram.Execute).Run();
            Assert.AreEqual(1, engine.NumOfFoundBugs);
        }
    }
}
