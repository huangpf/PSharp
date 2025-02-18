﻿//-----------------------------------------------------------------------
// <copyright file="MonitorDeclarationParser.cs">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# monitor declaration parsing visitor.
    /// </summary>
    internal sealed class MonitorDeclarationParser : BaseMachineVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal MonitorDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns true if the given class declaration is a machine.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        protected override bool IsMachine(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMonitor(compilation, classDecl);
        }

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMonitorState(compilation, classDecl);
        }

        /// <summary>
        /// Returns the type of the machine.
        /// </summary>
        /// <returns>Text</returns>
        protected override string GetTypeOfMachine()
        {
            return "Monitor";
        }

        #endregion
    }
}
