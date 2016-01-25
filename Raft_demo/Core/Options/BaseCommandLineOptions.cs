﻿//-----------------------------------------------------------------------
// <copyright file="BaseCommandLineOptions.cs">
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

namespace Microsoft.PSharp.Utilities
{
    public abstract class BaseCommandLineOptions
    {
        #region fields

        protected Configuration Configuration;

        protected string[] Options;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="args">Array of arguments</param>
        public BaseCommandLineOptions(string[] args)
        {
            this.Options = args;
        }

        /// <summary>
        /// Parses the command line options and returns a configuration.
        /// </summary>
        /// <returns>Configuration</returns>
        public Configuration Parse()
        {
            for (int idx = 0; idx < this.Options.Length; idx++)
            {
                this.ParseOption(this.Options[idx]);
            }

            this.CheckForParsingErrors();
            return Configuration;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Parse the given option.
        /// </summary>
        /// <param name="option">Option</param>
        protected virtual void ParseOption(string option)
        {
            if (option.ToLower().Equals("/?"))
            {
                this.ShowHelp();
                Environment.Exit(0);
            }
            else if (option.ToLower().StartsWith("/s:") && option.Length > 3)
            {
                this.Configuration.SolutionFilePath = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/p:") && option.Length > 3)
            {
                this.Configuration.ProjectName = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/o:") && option.Length > 3)
            {
                this.Configuration.OutputFilePath = option.Substring(3);
            }
            else if (option.ToLower().StartsWith("/v:") && option.Length > 3)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(3), out i) && i >= 0 && i <= 3)
                {
                    ErrorReporter.ReportAndExit("Please give a valid verbosity level " +
                        "'/v:[x]', where 0 <= [x] <= 3.");
                }

                this.Configuration.Verbose = i;
            }
            else if (option.ToLower().Equals("/debug"))
            {
                IO.Debugging = true;
            }
            else if (option.ToLower().Equals("/showwarnings"))
            {
                ErrorReporter.ShowWarnings = true;
            }
            else if (option.ToLower().StartsWith("/timeout:") && option.Length > 9)
            {
                int i = 0;
                if (!int.TryParse(option.Substring(9), out i) &&
                    i > 0)
                {
                    ErrorReporter.ReportAndExit("Please give a valid timeout " +
                        "'/timeout:[x]', where [x] > 0.");
                }

                this.Configuration.Timeout = i;
            }
            else if (option.ToLower().Equals("/interop"))
            {
                Configuration.InteroperationEnabled = true;
            }
            else
                {
                this.ShowHelp();
                ErrorReporter.ReportAndExit("cannot recognise command line option '" +
                    option + "'.");
            }
        }

        /// <summary>
        /// Checks for parsing errors.
        /// </summary>
        protected abstract void CheckForParsingErrors();

        /// <summary>
        /// Shows help.
        /// </summary>
        protected abstract void ShowHelp();

        #endregion
    }
}
