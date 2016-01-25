﻿//-----------------------------------------------------------------------
// <copyright file="SchedulingStrategy.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.PSharp.Utilities
{
    /// <summary>
    /// P# runtime scheduling strategy.
    /// </summary>
    public enum SchedulingStrategy
    {
        /// <summary>
        /// Interactive scheduling.
        /// </summary>
        Interactive = 0,
        /// <summary>
        /// Random scheduling.
        /// </summary>
        Random,
        /// <summary>
        /// Depth-first search scheduling.
        /// </summary>
        DFS,
        /// <summary>
        /// Depth-first search scheduling with
        /// iterative deepening.
        /// </summary>
        IDDFS,
        /// <summary>
        /// Delay-bounding scheduling.
        /// </summary>
        DelayBounding,
        /// <summary>
        /// Random delay-bounding scheduling.
        /// </summary>
        RandomDelayBounding,
        /// <summary>
        /// Prioritized scheduling.
        /// </summary>
        PCT,
        /// <summary>
        /// Random operation-bounding scheduling.
        /// </summary>
        RandomOperationBounding,
        /// <summary>
        /// Prioritized operation-bounding scheduling.
        /// </summary>
        PrioritizedOperationBounding,
        /// <summary>
        /// MaceMC based search scheduling to detect
        /// potential liveness violations.
        /// </summary>
        MaceMC
    }
}
