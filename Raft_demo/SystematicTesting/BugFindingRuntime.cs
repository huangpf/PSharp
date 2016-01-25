﻿//-----------------------------------------------------------------------
// <copyright file="BugFindingRuntime.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.SystematicTesting.Exploration;
using Microsoft.PSharp.SystematicTesting.Scheduling;
using Microsoft.PSharp.SystematicTesting.StateCaching;
using Microsoft.PSharp.Threading;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting
{
    /// <summary>
    /// Static class implementing the P# bug-finding runtime.
    /// </summary>
    internal sealed class PSharpBugFindingRuntime : PSharpRuntime
    {
        #region fields
        
        /// <summary>
        /// List of machine tasks.
        /// </summary>
        private List<Task> MachineTasks;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        /// <summary>
        /// Lock used by the runtime.
        /// </summary>
        private Object Lock = new Object();

        /// <summary>
        /// The P# program trace.
        /// </summary>
        internal Trace ProgramTrace;

        /// <summary>
        /// The P# task scheduler.
        /// </summary>
        internal TaskScheduler TaskScheduler;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal int? RootTaskId;

        /// <summary>
        /// The P# bugfinding scheduler.
        /// </summary>
        internal BugFindingScheduler BugFinder;

        /// <summary>
        /// The P# program state cache.
        /// </summary>
        internal StateCache StateCache;

        /// <summary>
        /// The P# liveness checker.
        /// </summary>
        internal LivenessChecker LivenessChecker;

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private int OperationIdCounter;

        /// <summary>
        /// True if runtime is running.
        /// </summary>
        private bool IsRunning = false;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// <param name="configuration">Configuration</param>
        /// <param name="strategy">SchedulingStrategy</param>
        internal PSharpBugFindingRuntime(Configuration configuration, ISchedulingStrategy strategy)
            : base(configuration)
        {
            this.RootTaskId = Task.CurrentId;

            this.MachineTasks = new List<Task>();
            this.Monitors = new List<Monitor>();

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                this.TaskScheduler = new TaskWrapperScheduler(this, this.MachineTasks);
                TaskMachineExtensions.TaskScheduler = this.TaskScheduler as TaskWrapperScheduler;
                this.BugFinder = new TaskAwareBugFindingScheduler(this, strategy);
            }
            else
            {
                this.BugFinder = new BugFindingScheduler(this, strategy);
            }

            this.ProgramTrace = new Trace();
            this.StateCache = new StateCache(this);
            this.LivenessChecker = new LivenessChecker(this);

            this.OperationIdCounter = 0;

            this.IsRunning = true;
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        public override void SendEvent(MachineId target, Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");

            try
            {
                this.Send(null, target, e, false);
            }
            catch (TaskCanceledException)
            {
                IO.Debug("<Exception> TaskCanceledException was thrown.");
            }
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        public override void InvokeMonitor<T>(Event e)
        {
            // If the event is null then report an error and exit.
            this.Assert(e != null, "Cannot send a null event.");
            this.Monitor<T>(null, e);
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                bool killTasks = true;
                if (Task.CurrentId == this.RootTaskId)
                {
                    killTasks = false;
                }

                string message = "Assertion failure.";
                this.BugFinder.NotifyAssertionFailure(message, killTasks);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                bool killTasks = true;
                if (Task.CurrentId == this.RootTaskId)
                {
                    killTasks = false;
                }

                string message = IO.Format(s, args);
                this.BugFinder.NotifyAssertionFailure(message, killTasks);
            }
        }

        /// <summary>
        /// Logs the given text.
        /// </summary>
        /// <param name="s">String</param>
        /// <param name="args">Arguments</param>
        public override void Log(string s, params object[] args)
        {
            IO.Log(s, args);
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateMachine(Type type)
        {
            if (type.IsSubclassOf(typeof(Machine)))
            {
                MachineId mid = new MachineId(type, this);
                Object machine = Activator.CreateInstance(type);
                (machine as Machine).SetMachineId(mid);
                (machine as Machine).InitializeStateInformation();
                
                if (!this.MachineMap.TryAdd(mid.Value, machine as Machine))
                {
                    this.Assert(false, "Machine {0}({1}) was already created.", type.Name, mid.Value);
                }

                IO.Log("<CreateLog> Machine {0}({1}) is created.", type.Name, mid.MVal);

                Task task = new Task(() =>
                {
                    this.BugFinder.NotifyTaskStarted();
                    
                    (machine as Machine).GotoStartState();
                    (machine as Machine).RunEventHandler();

                    this.BugFinder.NotifyTaskCompleted();
                });

                lock (this.Lock)
                {
                    this.MachineTasks.Add(task);
                    this.TaskMap.TryAdd(task.Id, machine as Machine);
                }

                this.BugFinder.NotifyNewTaskCreated(task.Id, machine as Machine);

                if (this.Configuration.ScheduleIntraMachineConcurrency)
                {
                    task.Start(this.TaskScheduler);
                }
                else
                {
                    task.Start();
                }

                this.BugFinder.WaitForTaskToStart(task.Id);
                this.BugFinder.Schedule();

                return mid;
            }
            else
            {
                this.Assert(false, "Type '{0}' is not a machine.", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new monitor of the given type.
        /// </summary>
        /// <param name="type">Type of the monitor</param>
        internal override void TryCreateMonitor(Type type)
        {
            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a " +
                "subclass of Monitor.\n", type.Name);

            MachineId mid = new MachineId(type, this);
            Object monitor = Activator.CreateInstance(type);
            (monitor as Monitor).SetMachineId(mid);
            (monitor as Monitor).InitializeStateInformation();

            IO.Log("<CreateLog> Monitor {0} is created.", type.Name);

            this.Monitors.Add(monitor as Monitor);

            if (this.Configuration.CheckLiveness)
            {
                this.LivenessChecker.RegisterMonitor(monitor as Monitor);
            }
            
            (monitor as Monitor).GotoStartState();
        }

        /// <summary>
        /// Tries to create a new task machine.
        /// </summary>
        /// <param name="userTask">Task</param>
        internal override void TryCreateTaskMachine(Task userTask)
        {
            this.Assert(this.TaskScheduler is TaskWrapperScheduler, "Unable to wrap " +
                "the task in a machine, because the task wrapper scheduler is not enabled.\n");

            MachineId mid = new MachineId(typeof(TaskMachine), this);
            TaskMachine taskMachine = new TaskMachine(this.TaskScheduler as TaskWrapperScheduler, userTask);
            taskMachine.SetMachineId(mid);
            
            IO.Log("<CreateLog> TaskMachine({0}) is created.", mid.MVal);

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();
                taskMachine.Run();
                this.BugFinder.NotifyTaskCompleted();
            });

            lock (this.Lock)
            {
                this.MachineTasks.Add(task);
            }

            this.BugFinder.NotifyNewTaskCreated(task.Id, taskMachine);

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(this.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            this.BugFinder.WaitForTaskToStart(task.Id);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void Send(AbstractMachine sender, MachineId mid, Event e, bool isStarter)
        {
            this.Assert(mid != null, "Cannot send to a null machine.");
            this.Assert(e != null, "Cannot send a null event.");

            e.SetSenderMachine(sender.Id);
            this.SetOperationIdForEvent(e, sender, isStarter);

            if (this.Configuration.BoundOperations && sender != null)
            {
                IO.Log("<SendLog> Machine '{0}({1})' sent event '{2}({3})' to '{4}({5})'.",
                    sender, sender.Id.MVal, e.GetType().FullName, e.OperationId, mid.Type, mid.MVal);
            }
            else if (sender != null)
            {
                IO.Log("<SendLog> Machine '{0}({1})' sent event '{2}' to '{3}({4})'.",
                    sender, sender.Id.MVal, e.GetType().FullName, mid.Type, mid.MVal);
            }
            else
            {
                IO.Log("<SendLog> Event '{0}' was sent to '{1}({2})'.",
                    e.GetType().FullName, mid.Type, mid.MVal);
            }

            Machine machine = this.MachineMap[mid.Value];

            bool runNewHandler = false;
            machine.Enqueue(e, ref runNewHandler);
            
            if (!runNewHandler)
            {
                this.BugFinder.Schedule();
                return;
            }

            machine.SetOperationId(e.OperationId);

            Task task = new Task(() =>
            {
                this.BugFinder.NotifyTaskStarted();
                machine.RunEventHandler();
                this.BugFinder.NotifyTaskCompleted();
            });

            lock (this.Lock)
            {
                this.MachineTasks.Add(task);
                this.TaskMap.TryAdd(task.Id, machine as Machine);
            }

            this.BugFinder.NotifyNewTaskCreated(task.Id, machine);

            if (this.Configuration.ScheduleIntraMachineConcurrency)
            {
                task.Start(this.TaskScheduler);
            }
            else
            {
                task.Start();
            }

            this.BugFinder.WaitForTaskToStart(task.Id);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <param name="sender">Sender machine</param>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        internal override void Monitor<T>(AbstractMachine sender, Event e)
        {
            foreach (var m in this.Monitors)
            {
                if (m.GetType() == typeof(T))
                {
                    m.MonitorEvent(e);
                }
            }
        }

        /// <summary>
        /// Raises an event internally and returns from the execution context.
        /// </summary>
        /// <param name="raiser">Raiser machine</param>
        /// <param name="e">Event</param>
        /// <param name="isStarter">Is starting a new operation</param>
        internal override void Raise(AbstractMachine raiser, Event e, bool isStarter)
        {
            this.SetOperationIdForEvent(e, raiser, isStarter);

            if (this.Configuration.BoundOperations)
            {
                IO.Log("<RaiseLog> Machine '{0}({1})' raised event '{2}({3})'.",
                    raiser, raiser.Id.MVal, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<RaiseLog> Machine '{0}({1})' raised event '{2}'.",
                    raiser, raiser.Id.MVal, e.GetType().FullName);
            }
        }

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="maxValue">Max value</param>
        /// <returns>Boolean</returns>
        internal override bool GetNondeterministicChoice(AbstractMachine machine, int maxValue)
        {
            var choice = this.BugFinder.GetNextNondeterministicChoice(maxValue);
            IO.Log("<RandomLog> Machine '{0}({1})' nondeterministically chose '{2}'.",
                machine, machine.Id.MVal, choice);
            return choice;
        }

        /// <summary>
        /// Returns a fair nondeterministic boolean choice, that can be
        /// controlled during analysis or testing.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal override bool GetFairNondeterministicChoice(AbstractMachine machine, string uniqueId)
        {
            var choice = this.BugFinder.GetNextNondeterministicChoice(2, uniqueId);
            IO.Log("<RandomLog> Machine '{0}({1})' nondeterministically chose '{2}'.",
                machine, machine.Id.MVal, choice);
            return choice;
        }

        /// <summary>
        /// Notifies that a machine dequeued an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyDequeuedEvent(Machine machine, Event e)
        {
            if (this.Configuration.BoundOperations)
            {
                IO.Log("<DequeueLog> Machine '{0}({1})' dequeued event '{2}({3})'.",
                    machine, machine.Id.MVal, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<DequeueLog> Machine '{0}({1})' dequeued event '{2}'.",
                    machine, machine.Id.MVal, e.GetType().FullName);
            }
            
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(e.OperationId);
            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine raised an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyRaisedEvent(Machine machine, Event e)
        {
            var prevMachineOpId = machine.OperationId;
            machine.SetOperationId(e.OperationId);
            //if (this.Configuration.BoundOperations && prevMachineOpId != machine.OperationId)
            //{
            //    this.BugFinder.Schedule();
            //}
        }

        /// <summary>
        /// Notifies that a machine is waiting to receive an event.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal override void NotifyWaitEvent(Machine machine)
        {
            this.BugFinder.NotifyTaskBlockedOnEvent(Task.CurrentId);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="e">Event</param>
        internal override void NotifyReceivedEvent(Machine machine, Event e)
        {
            if (this.Configuration.BoundOperations)
            {
                IO.Log("<ReceiveLog> Machine '{0}({1})' received event '{2}({3})' and unblocked.",
                    machine, machine.Id.MVal, e.GetType().FullName, e.OperationId);
            }
            else
            {
                IO.Log("<ReceiveLog> Machine '{0}({1})' received event '{2}' and unblocked.",
                    machine, machine.Id.MVal, e.GetType().FullName);
            }

            this.BugFinder.NotifyTaskReceivedEvent(machine);
        }

        /// <summary>
        /// Notifies that a default handler has been used.
        /// </summary>
        internal override void NotifyDefaultHandlerFired()
        {
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Notifies that a scheduling point should be instrumented
        /// due to a wait synchronization operation.
        /// </summary>
        /// <param name="blockingTasks">Blocking tasks</param>
        /// <param name="waitAll">Boolean</param>
        internal void ScheduleOnWait(IEnumerable<Task> blockingTasks, bool waitAll)
        {
            this.Assert(this.BugFinder is TaskAwareBugFindingScheduler,
                "Cannot schedule on wait without enabling the task-aware bug finding scheduler.");
            (this.BugFinder as TaskAwareBugFindingScheduler).NotifyTaskBlocked(
                Task.CurrentId, blockingTasks, waitAll);
            this.BugFinder.Schedule();
        }

        /// <summary>
        /// Returns the fingerprint of the current program state.
        /// </summary>
        /// <returns>Fingerprint</returns>
        internal Fingerprint GetProgramState()
        {
            Fingerprint fingerprint = null;

            unchecked
            {
                int hash = 19;

                foreach (var machine in this.MachineMap.Values)
                {
                    hash = hash + 31 * machine.GetCachedState();
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = hash + 31 * monitor.GetCachedState();
                }

                fingerprint = new Fingerprint(hash);
            }

            return fingerprint;
        }

        /// <summary>
        /// Waits until all P# machines have finished execution.
        /// </summary>
        internal void WaitMachines()
        {
            Task[] taskArray = null;

            while (this.IsRunning)
            {
                lock (this.Lock)
                {
                    taskArray = this.MachineTasks.ToArray();
                }

                try
                {
                    Task.WaitAll(taskArray);
                }
                catch (AggregateException)
                {
                    break;
                }

                bool moreTasksExist = false;
                lock (this.Lock)
                {
                    moreTasksExist = taskArray.Length != this.MachineTasks.Count;
                }

                if (!moreTasksExist)
                {
                    break;
                }
            }

            this.IsRunning = false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Sets the operation id for the given event.
        /// </summary>
        /// <param name="e">Event</param>
        /// <param name="sender">Sender machine</param>
        /// <param name="isStarter">Is starting a new operation</param>
        private void SetOperationIdForEvent(Event e, AbstractMachine sender, bool isStarter)
        {
            if (isStarter)
            {
                this.OperationIdCounter++;
                e.SetOperationId(this.OperationIdCounter);
            }
            else if (sender != null)
            {
                e.SetOperationId(sender.OperationId);
            }
            else
            {
                e.SetOperationId(0);
            }
        }

        #endregion

        #region TPL methods

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public override void WaitAll(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAll(Task[] tasks, CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public override void WaitAll(Task[] tasks, int millisecondsTimeout)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public override void WaitAll(Task[] tasks, TimeSpan timeout)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, timeout);
        }

        /// <summary>
        /// Waits for all of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAll(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, true);
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        public override void WaitAny(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete execution.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAny(Task[] tasks, CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution
        /// within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        public override void WaitAny(Task[] tasks, int millisecondsTimeout)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, millisecondsTimeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="timeout">Timeout</param>
        public override void WaitAny(Task[] tasks, TimeSpan timeout)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, timeout);
        }

        /// <summary>
        /// Waits for any of the provided cancellable task objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public override void WaitAny(Task[] tasks, int millisecondsTimeout,
            CancellationToken cancellationToken)
        {
            this.ScheduleOnWait(tasks, false);
            Task.WaitAny(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task WhenAll(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task WhenAll(IEnumerable<Task> tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an array have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when all of the task objects
        /// in an enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            this.ScheduleOnWait(tasks, true);
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task> WhenAny(params Task[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        /// <summary>
        /// Creates a task that will complete when any of the supplied
        /// tasks have completed.
        /// </summary>
        /// <param name="tasks">Tasks</param>
        /// <returns>Task</returns>
        public override Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks)
        {
            this.ScheduleOnWait(tasks, false);
            return Task.WhenAny(tasks);
        }

        #endregion
    }
}
