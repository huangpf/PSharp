using System.Threading;
using Microsoft.PSharp;

namespace PSharpAlternative
{
    public class PSharpManager
    {
        public static void MachineTaskBody(MachineInfo machineInfo, object initialPayload, IPSharpInternal psi)
        {
            machineInfo.payload = initialPayload;

            psi.MachineStart(machineInfo);

            machineInfo.currentState = machineInfo.initialState;
            machineInfo.currentState.entryAction?.Invoke();

            while (true)
            {
                EventInfo nextEvent;

                if (machineInfo.raisedEvent != null)
                {
                    nextEvent = machineInfo.raisedEvent;
                    machineInfo.raisedEvent = null;
                }
                else 
                {
                    if (machineInfo.nextEvent == null)
                    {
                        psi.WaitForNextEvent(machineInfo);
                    }

                    nextEvent = machineInfo.nextEvent;
                    machineInfo.nextEvent = null;
                }

                Safety.Assert(nextEvent != null);

                if (nextEvent.type == typeof(Halt))
                {
                    break;
                }

                var action = machineInfo.currentState.GetActionBinding(nextEvent);
                var newStateType = machineInfo.currentState.GetStateChange(nextEvent);

                Safety.Assert(action != null ||
                    newStateType != null, "Machine received event that cannot be handled.");

                machineInfo.payload = nextEvent.payload;
                machineInfo.trigger = nextEvent.type;

                action?.Invoke();

                if (newStateType != null)
                {
                    var newState = machineInfo.states[newStateType];
                    machineInfo.currentState.exitAction?.Invoke();
                    machineInfo.currentState = newState;
                    machineInfo.currentState.entryAction?.Invoke();
                }
            }

            psi.MachineEnd(machineInfo);
        }




    }
}