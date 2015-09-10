namespace PSharpAlternative
{
    public class MachineSchedInfo
    {
        public MachineInfo machineInfo;
        public bool enabled;
        public bool active;
        public bool terminated;

        public MachineSchedInfo(MachineInfo machineInfo)
        {
            this.machineInfo = machineInfo;
        }
    }
}