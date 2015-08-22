using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.DynamicAnalysis.Scheduling
{
    public class DbRandStrategy : ISchedulingStrategy
    {
        private int maxSteps;
        private readonly long delayBound;
        private int delayCount;
        private int currentStep;
        private List<int> delays;

        private int seed;
        private Random random;

        public DbRandStrategy(int delayBound, int seed)
        {
            this.delayBound = delayBound;
            this.seed = seed;

            delays = new List<int>();

            Reset();
        }

        #region Implementation of ISchedulingStrategy

        public bool TryGetNext(out TaskInfo next, List<TaskInfo> tasks, TaskInfo currentTask)
        {
            var orderedTasks = OrderedTaskList(tasks, currentTask);
            var availableTasks =
                orderedTasks.Where(
                    info => info.IsEnabled && !info.IsWaiting && !info.IsBlocked).ToList();

            if (availableTasks.Count == 0)
            {
                next = null;
                return false;
            }

            int index = 0;

            while (delays.Count > 0 &&
                currentStep == delays[0])
            {
                //index = (index + 1) % availableTasks.Count;
                index = random.Next(availableTasks.Count);
                delays.RemoveAt(0);
            }

            currentStep++;

            next = availableTasks[index];
            return true;
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

        public int GetDepthBound()
        {
            return Configuration.DepthBound;
        }

        public bool HasReachedDepthBound()
        {
            if (GetDepthBound() == 0)
            {
                return false;
            }

            return currentStep == GetDepthBound();
        }

        public bool HasFinished()
        {
            return false;
        }

        public void ConfigureNextIteration()
        {
            maxSteps = Math.Max(maxSteps, currentStep);
            Console.WriteLine("---------------------New max steps: " + maxSteps);
            delayCount = 0;
            currentStep = 0;
            delays.Clear();
            for (int i = 0; i < delayBound; ++i)
            {
                delays.Add(random.Next(maxSteps));
            }
            delays.Sort();
        }

        public void Reset()
        {
            currentStep = 0;
            random = new Random(seed);
            ConfigureNextIteration();
        }

        public string GetDescription()
        {
            return "Randomized delay bounding (seed = " + seed + ")";
        }

        #endregion

        private List<TaskInfo> OrderedTaskList(
            List<TaskInfo> tasks,
            TaskInfo currentTask)
        {
            List<TaskInfo> res = new List<TaskInfo>();
            int size = tasks.Count;
            int curr = tasks.IndexOf(currentTask);
            for (int i = 0; i < size; ++i)
            {
                res.Add(tasks[(curr + i)%size]);
            }
            return res;
        } 

    }
}