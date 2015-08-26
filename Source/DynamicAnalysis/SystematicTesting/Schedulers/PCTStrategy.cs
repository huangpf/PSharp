using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.DynamicAnalysis.SystematicTesting.Schedulers
{
    class PCTStrategy : ISchedulingStrategy
    {
        private int currentStep;
        private List<MachineId> priorityList = new List<MachineId>();

        private int seed;
        private Random random;

        public PCTStrategy(int delayBound, int seed)
        {
            this.seed = seed;
            Reset();
        }
        public void ConfigureNextIteration()
        {
            priorityList.Clear();
            currentStep = 0;
        }

        public int GetDepthBound()
        {
            return Configuration.DepthBound;
        }

        public string GetDescription()
        {
            return "Probabilistic Concurrency Testing (seed = " + seed + ")";
        }

        public bool GetNextChoice(out bool next)
        {
            next = false;
            if (random.Next(2) == 1)
            {
                next = true;
            }
            return true;
        }

        public int GetSchedulingSteps()
        {
            return currentStep;
        }

        public bool HasFinished()
        {
            return false;
        }

        public bool HasReachedDepthBound()
        {
            if (GetDepthBound() == 0)
            {
                return false;
            }

            return currentStep == GetDepthBound();
        }

        public void Reset()
        {
            currentStep = 0;
            random = new Random(seed);
            ConfigureNextIteration();
        }

        public bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks, TaskInfo currentTask)
        {
            var availableTasks =
                tasks.Where(
                    info => info.IsEnabled && !info.IsWaiting && !info.IsBlocked).ToList();
            if (availableTasks.Count == 0)
            {
                next = null;
                return false;
            }

            List<MachineId> newMachineIds = new List<MachineId>();

            foreach (TaskInfo item in availableTasks)
            {
                MachineId mid = item.Machine.Id;
                if (!priorityList.Contains(mid))
                {
                    newMachineIds.Add(mid);
                }
            }

            int newSize = priorityList.Count + newMachineIds.Count;
            int rnd = random.Next(priorityList.Count);
            for (int i = 0; i < newMachineIds.Count; i++)
            {
                int index = (rnd + i) % newSize;
                priorityList.Insert(index, newMachineIds[i]);
            }

            MachineId currentMid = currentTask.Machine.Id;
            priorityList.Remove(currentMid);
            priorityList.Insert(0, currentMid);

            currentStep++;


            MachineId nextMid = priorityList[priorityList.Count - 1];
            int j = 1;

            while (availableTasks.Where(info => info.Machine.Id.Equals(nextMid)).Count() == 0)
            {
                nextMid = priorityList[priorityList.Count - j];
                j++;
            }

            next = availableTasks.Where(info => info.Machine.Id.Equals(nextMid)).Single();
            PSharpRuntime.Assert(next.IsEnabled);
            return true;
        }
    }
}

