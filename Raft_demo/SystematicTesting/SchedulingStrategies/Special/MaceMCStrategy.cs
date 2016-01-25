﻿//-----------------------------------------------------------------------
// <copyright file="MaceMCStrategy.cs" company="Microsoft">
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
    /// Class representing a depth-first search scheduling strategy
    /// that incorporates iterative deepening.
    /// </summary>
    public class MaceMCStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// The max depth.
        /// </summary>
        private int MaxDepth;

        /// <summary>
        /// The safety prefix depth.
        /// </summary>
        private int SafetyPrefixDepth;

        /// <summary>
        /// A bounded DFS strategy with interative deepening.
        /// </summary>
        private IterativeDeepeningDFSStrategy BoundedDFS;

        /// <summary>
        /// A random strategy.
        /// </summary>
        private RandomStrategy Random;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">Configuration</param>
        public MaceMCStrategy(Configuration configuration)
        {
            this.Configuration = configuration;
            this.MaxDepth = this.Configuration.DepthBound;
            this.SafetyPrefixDepth = this.Configuration.SafetyPrefixBound;
            this.BoundedDFS = new IterativeDeepeningDFSStrategy(configuration);
            this.Random = new RandomStrategy(configuration);
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
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.TryGetNext(out next, choices, current);
            }
            {
                return this.BoundedDFS.TryGetNext(out next, choices, current);
            }
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextChoice(int maxValue, out bool next)
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetNextChoice(maxValue, out next);
            }
            else
            {
                return this.BoundedDFS.GetNextChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetExploredSteps();
            }
            else
            {
                return this.BoundedDFS.GetExploredSteps();
            }
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            if (this.BoundedDFS.HasReachedDepthBound())
            {
                return this.Random.GetMaxExploredSteps();
            }
            else
            {
                return this.BoundedDFS.GetMaxExploredSteps();
            }
        }

        /// <summary>
        /// Returns the depth bound.
        /// </summary>
        /// <returns>Depth bound</returns>
        public int GetDepthBound()
        {
            return this.MaxDepth;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            return this.Random.HasReachedDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return this.BoundedDFS.HasFinished();
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.BoundedDFS.ConfigureNextIteration();
            this.Random.ConfigureNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.BoundedDFS.Reset();
            this.Random.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "";
        }

        #endregion
    }
}
