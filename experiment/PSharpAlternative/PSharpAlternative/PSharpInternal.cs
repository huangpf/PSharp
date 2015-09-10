using System.Threading;

namespace PSharpAlternative
{
    public class PSharpInternal : IPSharpInternal
    {
        #region Implementation of IPSharpInternal

        public void WaitForNextEvent(MachineInfo machineInfo)
        {
            lock (machineInfo)
            {
                while (!TryMakeNextEventNonNull(machineInfo))
                {
                    Monitor.Wait(machineInfo);
                }
            }
        }

        public void MachineStart(MachineInfo machineInfo)
        {
            
        }

        public void MachineEnd(MachineInfo machineInfo)
        {
        }

        #endregion
        
    }
}