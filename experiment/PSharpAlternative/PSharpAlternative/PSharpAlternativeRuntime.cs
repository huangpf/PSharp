using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PSharp;
using PSharpAlternative.Scheduling;

namespace PSharpAlternative
{
    public class PSharpAlternativeRuntime : IPSharpRuntime, IPSharpInternal
    {
        private static class ThreadLocals
        {
            [ThreadStatic]
            public static MachineSchedInfo currentMachine;
        }

        private readonly MachineSet machineSet;

        public PSharpAlternativeRuntime()
        {
            machineSet = new MachineSet();
        }

        #region Implementation of IPSharpRuntime

        public MachineId CreateMachine(Type type, params object[] payload)
        {
            Machine machine = (Machine) Activator.CreateInstance(type);

            var machineInfo = machineSet.Add(machine);

            Schedule(OpType.MachineCreate);

            var machineSchedInfo = machineSet.GetMachineSchedInfo(machineInfo.id);
            machineSchedInfo.enabled = true;

            Task.Factory.StartNew(() =>
            {
                ThreadLocals.currentMachine = machineSchedInfo;

                Schedule(OpType.MachineStart);
                PSharpManager.MachineTaskBody(
                    machineInfo,
                    payload,
                    this);
                Schedule(OpType.MachineEnd);
            });

            return machineInfo.id;
        }

        public void Send(MachineId target, Event e, params object[] payload)
        {
            Schedule(OpType.Send);
            var targetSchedInfo = machineSet.GetMachineSchedInfo(target);
            targetSchedInfo.machineInfo.inbox.Add(new EventInfo(e, payload));

            if (!targetSchedInfo.enabled &&
                !targetSchedInfo.terminated &&
                targetSchedInfo.machineInfo.TryMakeNextEventNonNull())
            {
                targetSchedInfo.enabled = true;
            }
        }

        public void Receive(Predicate<Event> eventPredicate)
        {
            Schedule(OpType.ReceieveManual);
            throw new NotImplementedException();
        }

        public void InvokeMonitor<T>(object e, params object[] payload)
        {
            throw new NotImplementedException();
        }

        public bool Nondeterministic()
        {
            throw new NotImplementedException();
        }

        public void Assert(bool predicate)
        {
            Safety.Assert(predicate);
        }

        public void Assert(bool predicate, string s, params object[] args)
        {
            Safety.Assert(predicate, s, args);
        }

        public MachineId CurrentId()
        {
            return ThreadLocals.currentMachine.machineInfo.id;
        }

        public object CurrentPayload()
        {
            return ThreadLocals.currentMachine.machineInfo.payload;
        }

        public Type CurrentTrigger()
        {
            return ThreadLocals.currentMachine.machineInfo.trigger;
        }

        public void Raise(Event e, params object[] payload)
        {
            var machineInfo = ThreadLocals.currentMachine.machineInfo;
            Safety.Assert(machineInfo.raisedEvent == null,
                "Machine tried to raise an event more than once.");

            if (machineInfo.currentState.IsIgnored(e.GetType()))
            {
                return;
            }

            machineInfo.raisedEvent = new EventInfo(e, payload);
        }

        #endregion

        #region Implementation of IPSharpInternal

        public void WaitForNextEvent(MachineInfo machineInfo)
        {
            Safety.Assert(machineInfo.nextEvent == null);
            var machineSchedInfo = machineSet.GetMachineSchedInfo(machineInfo.id);

            machineSchedInfo.enabled = false;
            Schedule(OpType.Receive);

            Safety.Assert(machineInfo.nextEvent != null);
        }

        public void MachineStart(MachineInfo machineInfo)
        {
            var machineSchedInfo = machineSet.GetMachineSchedInfo(machineInfo.id);

            lock (machineSchedInfo)
            {
                while (machineSchedInfo.active != true)
                {
                    Monitor.Wait(machineSchedInfo);
                }

                if (machineSchedInfo.terminated)
                {
                    throw new TerminatedException();
                }
            }
        }

        public void MachineEnd(MachineInfo machineInfo)
        {
        }

        #endregion

        private static void Schedule(OpType opType)
        {
            
        }
    }
}