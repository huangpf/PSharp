using System;

namespace Microsoft.PSharp
{
    public interface IPSharpRuntime
    {
        /// <summary>
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns>
        MachineId CreateMachine(Type type, params object[] payload);

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        void Send(MachineId target, Event e, params object[] payload);

        /// <summary>
        /// Blocks and waits to receive an event of the given types. Returns
        /// a payload, if there is any, else returns null.
        /// </summary>
        /// <returns>Payload</returns>
        void Receive(Predicate<Event> eventPredicate);

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor</typeparam>
        /// <param name="e">Event</param>
        /// <param name="payload">Optional payload</param>
        void InvokeMonitor<T>(object e, params object[] payload);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled
        /// during analysis or testing.
        /// </summary>
        /// <returns>Boolean</returns>
        bool Nondeterministic();

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        void Assert(bool predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not it reports
        /// an error and exits.
        /// </summary>
        /// <param name="predicate">Predicate</param>
        /// <param name="s">Message</param>
        /// <param name="args">Message arguments</param>
        void Assert(bool predicate, string s, params object[] args);

        MachineId CurrentId();

        object CurrentPayload();

        Type CurrentTrigger();

        void Raise(Event e, params object[] payload);

    }
}