namespace PSharpAlternative
{
    public interface IPSharpInternal
    {
        void WaitForNextEvent(MachineInfo machineInfo);
        void MachineStart(MachineInfo machineInfo);
        void MachineEnd(MachineInfo machineInfo);
    }
}