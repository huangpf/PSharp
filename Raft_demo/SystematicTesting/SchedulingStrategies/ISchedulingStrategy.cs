﻿//-----------------------------------------------------------------------
// <copyright file="ISchedulingStrategy.cs" company="Microsoft">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Interface of a generic machine scheduling strategy.
    /// </summary>
    public interface ISchedulingStrategy
    {
        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        bool TryGetNext(out MachineInfo next, IList<MachineInfo> choices, MachineInfo current);

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        bool GetNextChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        int GetExploredSteps();

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        int GetMaxExploredSteps();

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        int GetDepthBound();

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        bool HasReachedDepthBound();

        /// <summary>
        /// True if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        bool HasFinished();

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        void ConfigureNextIteration();

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string GetDescription();
    }
}
