using System;
using System.Collections.Generic;

namespace Visavi
{
    /// <summary>
    /// SCPI error exception
    /// </summary>
    public class ScpiErrorException : Exception
    {
        /// <summary>
        /// Default contructor
        /// </summary>
        public ScpiErrorException() : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public ScpiErrorException(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        public ScpiErrorException(int code, string message) : base(message)
        {
            this.HResult = code;
        }

        /// <summary>
        /// Stack trace.
        /// </summary>
        public override string StackTrace
        {
            get
            {
                // remove ScpiExtensions from stack trace
                var stackTrace = new List<string>();
                stackTrace.AddRange(base.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None));
                //stackTrace.RemoveAll(x => x.Contains(typeof(ScpiExtensions).Name));
                return string.Join(Environment.NewLine, stackTrace.ToArray());
            }
        }

        public string ResourceName
        {
            get
            {
                if (Data.Contains("ResourceName"))
                    return Data["ResourceName"] as string;
                return "";
            }
        }

        public string Context
        {
            get
            {
                if (Data.Contains("Context"))
                    return Data["Context"] as string;
                return "";
            }
        }
    }
}
