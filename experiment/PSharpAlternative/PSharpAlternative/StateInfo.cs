using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.PSharp;

namespace PSharpAlternative
{
    public class StateInfo
    {
        public Type stateType;
        public Action entryAction;
        public Action exitAction;

        public HashSet<Type> ignoredEvents;
        public HashSet<Type> deferredEvents;
        public Dictionary<Type, Action> actionBindings;
        public Dictionary<Type, Tuple<Type, Action>> gotoTransitions;

        public StateInfo(Type stateType, Machine machine)
        {
            this.stateType = stateType;
            var machineType = machine.GetType();

            var entryAttribute = this.stateType.GetCustomAttribute(typeof(OnEntry), false) as OnEntry;
            var exitAttribute = this.stateType.GetCustomAttribute(typeof(OnExit), false) as OnExit;

            if (entryAttribute != null)
            {
                var method = machineType.GetMethod(entryAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), machine, method);
                entryAction = action;
            }

            if (exitAttribute != null)
            {
                var method = machineType.GetMethod(exitAttribute.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action = (Action)Delegate.CreateDelegate(typeof(Action), machine, method);
                exitAction = action;
            }

            var gotoAttributes = stateType.GetCustomAttributes(typeof(OnEventGotoState), false)
                as OnEventGotoState[];
            var doAttributes = stateType.GetCustomAttributes(typeof(OnEventDoAction), false)
                as OnEventDoAction[];

            foreach (var attr in gotoAttributes)
            {
                if (attr.Action == null)
                {
                    gotoTransitions.Add(attr.Event,
                        new Tuple<Type, Action>(attr.State, null));
                }
                else
                {
                    var method = machineType.GetMethod(attr.Action,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var action =
                        (Action)
                            Delegate.CreateDelegate(typeof(Action),
                                machine,
                                method);
                    gotoTransitions.Add(attr.Event,
                        new Tuple<Type, Action>(attr.State, action));
                }
            }

            foreach (var attr in doAttributes)
            {
                var method = machineType.GetMethod(attr.Action,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var action =
                    (Action)
                        Delegate.CreateDelegate(typeof(Action), machine, method);
                actionBindings.Add(attr.Event, action);
            }

            var ignoreEventsAttribute =
                this.GetType().GetCustomAttribute(typeof(IgnoreEvents), false)
                    as IgnoreEvents;
            var deferEventsAttribute =
                this.GetType().GetCustomAttribute(typeof(DeferEvents), false)
                    as DeferEvents;

            if (ignoreEventsAttribute != null)
            {
                ignoredEvents.UnionWith(ignoreEventsAttribute.Events);
            }

            if (deferEventsAttribute != null)
            {
                deferredEvents.UnionWith(deferEventsAttribute.Events);
            }
        }

        public bool IsIgnored(Type eventType)
        {
            return !ignoredEvents.Contains(eventType);
        }

        public Type GetStateChange(EventInfo ei)
        {
            if (gotoTransitions.ContainsKey(ei.type))
            {
                var actionState = gotoTransitions[ei.type];
                return actionState.Item1;
            }
            return null;
        }

        public Action GetActionBinding(EventInfo ei)
        {
            if (gotoTransitions.ContainsKey(ei.type))
            {
                var actionState = gotoTransitions[ei.type];
                return actionState.Item2;
            }

            if (actionBindings.ContainsKey(ei.type))
            {
                var action = actionBindings[ei.type];
                return action;
            }

            return null;
        }

    }
}