using System.Collections.Generic;
using Microsoft.PSharp;

namespace PSharpAlternative
{
    public class MachineSet
    {
        private readonly Dictionary<MachineId, MachineInfo> machines;
        private int nextMachineId;

        private readonly List<MachineSchedInfo> machineSchedInfos;

        public MachineInfo Add(Machine machine)
        {
            var machineInfo = new MachineInfo(
                new MachineId(nextMachineId++),
                machine);

            machines.Add(machineInfo.id, machineInfo);
            machineSchedInfos.Add(new MachineSchedInfo(machineInfo));
            Safety.Assert(machineInfo.id.value == machineSchedInfos.Count - 1);

            return machineInfo;
        }

        public MachineInfo GetMachineInfo(MachineId machineId)
        {
            return machines[machineId];
        }

        public MachineSchedInfo GetMachineSchedInfo(MachineId machineId)
        {
            return machineSchedInfos[machineId.value];
        }

    }
}