﻿//-----------------------------------------------------------------------
// <copyright file="PCTStrategy.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing a probabilistic concurrency testing (PCT)
    /// scheduling strategy.
    /// </summary>
    public class PCTStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        private int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        /// <summary>
        /// The bug depth.
        /// </summary>
        private int BugDepth;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        /// <summary>
        /// List of prioritized machines.
        /// </summary>
        private List<MachineId> PrioritizedMachines;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private SortedSet<int> PriorityChangePoints;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="depth">Bug depth</param>
        public PCTStrategy(Configuration configuration, int depth)
        {
            this.Configuration = configuration;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.BugDepth = depth;
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new Random(this.Seed);
            this.PrioritizedMachines = new List<MachineId>();
            this.PriorityChangePoints = new SortedSet<int>();
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IList<MachineInfo> choices, MachineInfo current)
        {
            var availableMachines = choices.Where(
                mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaiting).ToList();
            if (availableMachines.Count == 0)
            {
                next = null;
                return false;
            }

            next = this.GetPrioritizedMachine(availableMachines, current);
            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                next = true;
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public int GetDepthBound()
        {
            return this.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            if (this.Configuration.DepthBound == 0)
            {
                return false;
            }

            return this.ExploredSteps == this.GetDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;

            this.PrioritizedMachines.Clear();
            this.PriorityChangePoints.Clear();
            for (int idx = 0; idx < this.BugDepth - 1; idx++)
            {
                this.PriorityChangePoints.Add(this.Random.Next(this.MaxExploredSteps));
            }
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.Random = new Random(this.Seed);
            this.PrioritizedMachines.Clear();
            this.PriorityChangePoints.Clear();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            var text = this.BugDepth + "' bug depth, priority change points '[";

            int idx = 0;
            foreach (var points in this.PriorityChangePoints)
            {
                text += points;
                if (idx < this.PriorityChangePoints.Count - 1)
                {
                    text += ", ";
                }

                idx++;
            }

            text += "]'.";
            return text;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the prioritized machine.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>MachineInfo</returns>
        private MachineInfo GetPrioritizedMachine(List<MachineInfo> choices, MachineInfo current)
        {
            if (this.PrioritizedMachines.Count == 0)
            {
                this.PrioritizedMachines.Add(current.Machine.Id);
            }

            foreach (var mi in choices.Where(mi => !this.PrioritizedMachines.Contains(mi.Machine.Id)))
            {
                var mIndex = this.Random.Next(this.PrioritizedMachines.Count) + 1;
                this.PrioritizedMachines.Insert(mIndex, mi.Machine.Id);
                IO.Debug("<PCTDebug> Detected new machine '{0}({1})' at index '{2}'.",
                    mi.Machine, mi.Machine.Id.MVal, mIndex);
            }
            
            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                if (choices.Count == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledMachine(choices);
                    this.PrioritizedMachines.Remove(priority);
                    this.PrioritizedMachines.Add(priority);
                    IO.PrintLine("<PCTLog> Machine '{0}({1})' changes to lowest priority.",
                        priority.Type, priority.MVal);
                }
            }

            var prioritizedMachine = this.GetHighestPriorityEnabledMachine(choices);
            IO.Debug("<PCTDebug> Prioritized machine '{0}({1})'.",
                prioritizedMachine.Type, prioritizedMachine.MVal);
            if (IO.Debugging)
            {
                IO.Print("<PCTDebug> Priority list: ");
                for (int idx = 0; idx < this.PrioritizedMachines.Count; idx++)
                {
                    if (idx < this.PrioritizedMachines.Count - 1)
                    {
                        IO.Print("'{0}({1})', ", this.PrioritizedMachines[idx].Type,
                            this.PrioritizedMachines[idx].MVal);
                    }
                    else
                    {
                        IO.Print("'{0}({1})'.\n", this.PrioritizedMachines[idx].Type,
                            this.PrioritizedMachines[idx].MVal);
                    }
                }
            }

            var prioritizedMachineInfo = choices.First(mi => mi.Machine.Id.Equals(prioritizedMachine));
            return prioritizedMachineInfo;
        }

        /// <summary>
        /// Returns the highest-priority enabled machine.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>MachineId</returns>
        private MachineId GetHighestPriorityEnabledMachine(IEnumerable<MachineInfo> choices)
        {
            MachineId prioritizedMachine = null;
            foreach (var mid in this.PrioritizedMachines)
            {
                if (choices.Any(m => m.Machine.Id == mid))
                {
                    prioritizedMachine = mid;
                    break;
                }
            }

            return prioritizedMachine;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            this.PriorityChangePoints.Remove(this.ExploredSteps);
            var newPriorityChangePoint = this.ExploredSteps + 1;
            while (this.PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            this.PriorityChangePoints.Add(newPriorityChangePoint);
            IO.Debug("<PCTDebug> Moving priority change to '{0}'.", newPriorityChangePoint);
        }

        #endregion
    }
}
