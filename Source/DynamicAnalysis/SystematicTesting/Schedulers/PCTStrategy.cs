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
        private readonly List<MachineId> priorityList = new List<MachineId>();
        private readonly ISet<int> changePoints = new SortedSet<int>(); 

        private readonly int seed;
        private readonly int numChangePoints;
        private int maxSteps;
        private Random random;

        public PCTStrategy(int numChangePoints, int seed)
        {
            this.seed = seed;
            this.numChangePoints = numChangePoints;
            Reset();
        }

        public void ConfigureNextIteration()
        {
            priorityList.Clear();
            maxSteps = Math.Max(maxSteps, currentStep);
            currentStep = 0;

            changePoints.Clear();
            for (int i = 0; i < numChangePoints; ++i)
            {
                changePoints.Add(random.Next(maxSteps));
            }
        }

        public int GetDepthBound()
        {
            return Configuration.DepthBound;
        }

        public string GetDescription()
        {
            return "Probabilistic Concurrency Testing";//" (seed = " + seed + ")";
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
            maxSteps = 0;
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

            foreach (MachineId newMachineId in newMachineIds)
            {
                int index = random.Next(priorityList.Count+1);
                priorityList.Insert(index, newMachineId);
            }

            if (changePoints.Contains(currentStep))
            {
                MachineId currentMid = currentTask.Machine.Id;
                priorityList.Remove(currentMid);
                priorityList.Insert(0, currentMid);
            }

            currentStep++;

            // Start with highest priority machine.
            // Check if it is found in the enabled tasks list.
            // If not, decrement pli and try again.
            int pli = priorityList.Count - 1;
            int ati = -1;
            while (true)
            {

                ati = availableTasks.FindIndex(
                    ti => ti.Machine.Id.Equals(priorityList[pli]));
                if (ati != -1)
                {
                    break;
                }
                pli--;
                if (pli < 0)
                {
                    throw new Exception("Unexpected error in PCT scheduler");
                }
            }

            next = availableTasks[ati];

            PSharpRuntime.Assert(next.IsEnabled);
            PSharpRuntime.Assert(!next.IsBlocked);
            PSharpRuntime.Assert(!next.IsWaiting);

            return true;
        }
    }
}

