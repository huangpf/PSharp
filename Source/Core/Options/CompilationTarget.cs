﻿//-----------------------------------------------------------------------
// <copyright file="CompilationTarget.cs">
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
    /// P# compilation target.
    /// </summary>
    public enum CompilationTarget
    {
        /// <summary>
        /// Enables execution compilation target.
        /// </summary>
        Execution = 0,
        /// <summary>
        /// Enables testing compilation target.
        /// </summary>
        Testing = 1,
        /// <summary>
        /// Enables distribution compilation target.
        /// </summary>
        Distribution = 2
    }
}
