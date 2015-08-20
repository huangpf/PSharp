﻿//-----------------------------------------------------------------------
// <copyright file="Runtime.cs" company="Microsoft">
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Remote;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Static class implementing the P# runtime.
    /// </summary>
    public static class PSharpRuntime
    {
        #region fields

        /// <summary>
        /// A map from unique machine ids to machines.
        /// </summary>
        private static ConcurrentDictionary<int, Machine> MachineMap;

        /// <summary>
        /// Ip address.
        /// </summary>
        internal static string IpAddress;

        /// <summary>
        /// Port.
        /// </summary>
        internal static string Port;

        /// <summary>
        /// The application assembly.
        /// </summary>
        internal static Assembly AppAssembly;

        /// <summary>
        /// Channel for remote communication.
        /// </summary>
        internal static IRemoteCommunication Channel;

        #endregion

        #region public API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PSharpRuntime()
        {
            PSharpRuntime.MachineMap = new ConcurrentDictionary<int, Machine>();

            MachineId.ResetMachineIDCounter();

            var dispatcher = new Dispatcher();
            Microsoft.PSharp.Machine.Dispatcher = dispatcher;
            Microsoft.PSharp.Monitor.Dispatcher = dispatcher;

            PSharpRuntime.IpAddress = "";
            PSharpRuntime.Port = "";
        }

        /// <summary>
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        public static MachineId CreateMachine(Type type, params Object[] payload)
        {
            // TODO: remove
            Configuration.Debug.Contains(DebugType.Any);
            return PSharpRuntime.TryCreateMachine(type, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        public static void SendEvent(MachineId target, Event e, params Object[] payload)
        {
            // If the event is null then report an error and exit.
            PSharpRuntime.Assert(e != null, "Cannot send a null event.");
            e.AssignPayload(payload);
            PSharpRuntime.Send(target, e);
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        public static void InvokeMonitor<T>(Event e, params Object[] payload)
        {
            // the execution runtime does not implement monitors.
            return;
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool Nondeterministic()
        {
            return PSharpRuntime.Nondet();
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public static void Assert(bool predicate)
        {
            if (!predicate)
            {
                ErrorReporter.Report("Assertion failure.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public static void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                string message = Output.Format(s, args);
                ErrorReporter.Report(message);
                Environment.Exit(1);
            }
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new remote machine of the given type
        /// with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        internal static MachineId TryCreateMachineRemotely(Type type, params Object[] payload)
        {
            return PSharpRuntime.Channel.CreateMachine(type.FullName, payload);
        }

        /// <summary>
        /// Tries to create a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        internal static MachineId TryCreateMachine(Type type, params Object[] payload)
        {
            if (type.IsSubclassOf(typeof(Machine)))
            {
                Object machine = Activator.CreateInstance(type);

                var mid = (machine as Machine).Id;
                mid.IpAddress = PSharpRuntime.IpAddress;
                mid.Port = PSharpRuntime.Port;

                bool added = PSharpRuntime.MachineMap.TryAdd(mid.Value, machine as Machine);
                PSharpRuntime.Assert(added);
                
                Output.Debug(DebugType.Runtime, "<CreateLog> Machine {0}({1}) is created.",
                    type.Name, mid.Value);
                
                Task task = new Task(() =>
                {
                    (machine as Machine).AssignInitialPayload(payload);
                    (machine as Machine).GotoStartState();
                    (machine as Machine).RunEventHandler();
                });

                task.Start();

                return mid;
            }
            else
            {
                ErrorReporter.ReportAndExit("Type '{0}' is not a machine.", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new monitor of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        /// <param name="payload">Optional payload</param>
        internal static void TryCreateMonitor(Type type, params Object[] payload)
        {
            // the execution runtime does not implement monitors.
            return;
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal static void TryCreateTaskMachine(Task userTask)
        {
            // the execution runtime does not implement task machines.
            return;
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        internal static void SendRemotely(MachineId mid, Event e)
        {
            PSharpRuntime.Channel.SendEvent(mid, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="mid">Machine id</param>
        /// <param name="e">Event</param>
        internal static void Send(MachineId mid, Event e)
        {
            if (mid == null)
            {
                ErrorReporter.ReportAndExit("Cannot send to a null machine.");
            }
            else if (e == null)
            {
                ErrorReporter.ReportAndExit("Cannot send a null event.");
            }

            var machine = PSharpRuntime.MachineMap[mid.Value];

            var runHandler = false;
            machine.Enqueue(e, ref runHandler);

            if (!runHandler)
            {
                return;
            }

            Task task = new Task(() =>
            {
                machine.RunEventHandler();
            });

            task.Start();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal static void Monitor<T>(Event e)
        {
            // the execution runtime does not implement monitors.
            return;
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        internal static bool Nondet()
        {
            var random = new Random(DateTime.Now.Millisecond);

            bool result = false;
            if (random.Next(2) == 1)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        /// <param name="mid">Machine id</param>
        internal static void NotifyWaitEvent(MachineId mid)
        {
            var machine = PSharpRuntime.MachineMap[mid.Value];
            lock (machine)
            {
                System.Threading.Monitor.Wait(machine);
            }
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="mid">Machine id</param>
        internal static void NotifyReceivedEvent(MachineId mid)
        {
            var machine = PSharpRuntime.MachineMap[mid.Value];
            lock (machine)
            {
                System.Threading.Monitor.Pulse(machine);
            }
        }

        #endregion
    }
}
