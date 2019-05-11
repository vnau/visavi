using System.Globalization;

namespace Visavi
{
    /// <summary>
    /// SCPI error description
    /// </summary>
    public class ScpiError
    {
        /// <summary>
        /// Plain error message
        /// </summary>
        public string Plain { get; }

        /// <summary>
        /// SCPI error code
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// SCPI error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// SCPI error message context
        /// </summary>
        public string Context { get; }

        /// <summary>
        /// Instrument alias
        /// </summary>
        public string Alias { get; }

        /// <summary>
        /// Create ScpiError from plain error message
        /// </summary>
        /// <param name="text"></param>
        public ScpiError(string text, string alias = "", string context = "")
        {
            Plain = text;
            Alias = alias;
            Context = context;
            var d = text.Split(',');
            Code = int.Parse(d[0], CultureInfo.InvariantCulture);
            if (d.Length == 2)
            {
                Message = d[1].Trim(new[] { '\n', ' ', '"' });
            }
        }

        /// <summary>
        /// Create ScpiError from code end error message
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ScpiError(int code, string message, string alias = "", string context = "")
        {
            Alias = alias;
            Context = context;
            Plain = string.Format("{0}, \"{1}\"", code, message);
        }
    }
}
