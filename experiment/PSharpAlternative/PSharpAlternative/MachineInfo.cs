using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PSharp;

namespace PSharpAlternative
{
    public class MachineInfo
    {
        public readonly MachineId id;
        public readonly Machine machine;

        public readonly Dictionary<Type, StateInfo> states;
        public readonly StateInfo initialState;

        public readonly List<EventInfo> inbox;


        public Predicate<Event> waitPredicate;
         
        public EventInfo raisedEvent;
        public StateInfo currentState;

        /// <summary>
        /// Temporarily stores the next event taken from the inbox. Does not store raised events.
        /// </summary>
        public EventInfo nextEvent;

        public object payload;
        public Type trigger;

        public MachineInfo(MachineId machineId, Machine machine)
        {
            id = machineId;
            this.machine = machine;
            states = new Dictionary<Type, StateInfo>();
            currentState = null;
            inbox = new List<EventInfo>();

            Type machineType = machine.GetType();
            Type initialStateType = null;

            while (machineType != typeof(Machine))
            {
                foreach (var s in machineType.GetNestedTypes(BindingFlags.Instance |
                    BindingFlags.NonPublic | BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (s.IsClass && s.IsSubclassOf(typeof(MachineState)))
                    {
                        if (s.IsDefined(typeof(Start), false))
                        {
                            Safety.Assert(initialStateType == null, "Machine '{0}' can not have " +
                                "more than one start states.", machine.GetType().Name);
                            initialStateType = s;
                        }

                        Safety.Assert(s.BaseType == typeof(MachineState), "State '{0}' is " +
                            "not of the correct type.", s.Name);
                        states.Add(s, new StateInfo(s, machine));
                    }
                }

                machineType = machineType.BaseType;
            }
            Safety.Assert(initialStateType != null,
                "Machine '{0}' has no start state.",
                machine.GetType().Name);

            initialState = states[initialStateType];

        }

        public bool TryMakeNextEventNonNull()
        {
            if (nextEvent != null)
            {
                return true;
            }

            if (waitPredicate != null)
            {
                for (int i = 0; i < inbox.Count; ++i)
                {
                    EventInfo ei = inbox[i];
                    if (waitPredicate(ei.evt))
                    {
                        inbox.RemoveAt(i);
                        nextEvent = ei;
                        waitPredicate = null;
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < inbox.Count; ++i)
            {
                EventInfo ei = inbox[i];
                if (currentState.ignoredEvents.Contains(ei.type))
                {
                    inbox.RemoveAt(i);
                    i--;
                    continue;
                }
                if (currentState.deferredEvents.Contains(ei.type))
                {
                    continue;
                }

                nextEvent = ei;
                inbox.RemoveAt(i);
                break;
            }

            return nextEvent != null;
        }

    }
}