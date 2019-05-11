using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

#region visacom
#if VISACOM
using Ivi.Visa.Interop;
using IMessageInterface = Ivi.Visa.Interop.IMessage;
#else
using IMessageInterface = Ivi.Visa.IMessageBasedSession;
#endif
#endregion


namespace Visavi
{
    public enum MessageType
    {
        Send,
        Receive,
        Warning
    }

    public class MessageSessionContext
    {
        private string alias;
        private int? timeout;
        private MessageSessionContext baseContext;
        private bool checkScpiError;
        private bool ignoreWarnings;
        private Action<string, MessageType, string, string> action;
        private IMessageInterface messageInterface;

        protected void SetMessageInterface(IMessageInterface messageInterface)
        {
            this.messageInterface = messageInterface;
        }

        /// <summary>
        /// Defaut context constructor
        /// </summary>
        protected MessageSessionContext()
        {
            timeout = 10000;
            checkScpiError = true;
            ignoreWarnings = true;
        }

        protected MessageSessionContext(MessageSessionContext context)
        {
            alias = context.alias;
            timeout = context.timeout;
            messageInterface = context.messageInterface;
            baseContext = context;
            checkScpiError = context.checkScpiError;
            ignoreWarnings = context.ignoreWarnings;
        }

        virtual public IMessageInterface Session => messageInterface;

        public MessageSessionContext Log(Action<string, MessageType, string, string> action)
        {
            var context = new MessageSessionContext(this);
            context.action += action;
            return context;
        }

        /// <summary>
        /// Set alias for message session
        /// </summary>
        /// <param name="alias"></param>
        /// <returns>Message session context</returns>
        public MessageSessionContext WithAlias(string alias)
        {
            var context = new MessageSessionContext(this);
            context.alias = alias;
            return context;
        }

        /// <summary>
        /// Set read timeout for session
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public MessageSessionContext WithTimeout(int timeout)
        {
            var context = new MessageSessionContext(this);
            context.timeout = timeout;
            return context;
        }

        /// <summary>
        /// Query SYSTem:ERRor automatically and throw ScpiErrorException on error
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        public MessageSessionContext WithErrorsCheck(bool enable = true)
        {
            var context = new MessageSessionContext(this);
            context.checkScpiError = enable;
            return context;
        }

        public void Print(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(format, args);
                PrintNC(formattedQuery);
                if (checkScpiError)
                    ThrowExceptionOnError(formattedQuery);
            }
        }

        /// <summary>
        /// Read
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Read<T>()
        {
            return ReadNC<T>();
        }

        /// <summary>
        /// Query string
        /// </summary>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string Query(string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                var res = QueryNC<string>(formattedQuery);
                if (checkScpiError)
                    ThrowExceptionOnError(formattedQuery);
                return res;
            }
        }

        /// <summary>
        /// Query specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Query<T>(string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                var res = QueryNC<T>(formattedQuery);
                if (checkScpiError)
                    ThrowExceptionOnError(formattedQuery);
                return res;
            }
        }

        public T[] QueryArray<T>(string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                var res = QueryNCArray<T>(formattedQuery);
                if (checkScpiError)
                    ThrowExceptionOnError(formattedQuery);
                return res;
            }
        }

        private void LogCommandReceived(string command)
        {
            action?.Invoke(alias, MessageType.Receive, command, null);
        }

        private void LogCommandSent(string command)
        {
            action?.Invoke(alias, MessageType.Send, command, null);
        }

        private void LogWarning(string message, string context)
        {
            action?.Invoke(alias, MessageType.Warning, message, context);
        }


        private void PrintNC(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(format, args);
                LogCommandSent(formattedQuery);

#if VISACOM
                session.WriteString(formatted);
#else
                Session.FormattedIO.WriteLine(formattedQuery);
#endif
            }
        }

        /// <summary>
        /// Obtain disposable session lock
        /// </summary>
        /// <returns></returns>
        public IDisposable ObtainLock()
        {
            return new SessionLocker(Session);
        }


        /// <summary>
        /// Format SCPI array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string FormatArray<T>(IEnumerable<T> values) where T : IFormattable
        {
            return string.Join(",", values.Select(v => v.ToString(null, CultureInfo.InvariantCulture)));
        }


        /// <summary>
        /// Format string with invariant culture (decimal separator is dot)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns>formatted string</returns>
        private static string FormatString(string query, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, query, args);
        }


        private static T ConvertFromString<T>(string str)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return (T)typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, str);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="Context"></param>
        /// <param name="IgnoreWarnings"></param>
        private void ThrowExceptionOnError(ScpiErrorException exception, bool IgnoreWarnings = true)
        {
            if (exception.HResult != 0)
            {
                if (exception.HResult > 0) // warnings
                {
                    var instrument = exception.Data["Instrument"];
                    var context = exception.Data["Context"] as string;
                    LogWarning(exception.Message, context);
                }

                if (!IgnoreWarnings || exception.HResult < 0)
                    throw exception;
            }
        }

        /// <summary>
        /// Query SYSTem:ERRor and throw exception if error signaled.
        /// </summary>
        /// <param name="session"></param>
        public void ThrowExceptionOnError(string Context = null, bool IgnoreWarnings = true)
        {
            using (ObtainLock())
            {
                var exception = GetErrorException(Context);
                ThrowExceptionOnError(exception, IgnoreWarnings);

            }
        }

        public ScpiErrorException GetErrorException(string context = null)
        {
            using (ObtainLock())
            {
                /*
#if VISACOM
            var status = Session.ReadSTB();
#else
            var status = Session.ReadStatusByte();
#endif
*/
                //if (status.HasFlag(StatusByteFlags.EventStatusRegister) || status.HasFlag(StatusByteFlags.User2))
                {
                    var error = QueryError();
                    if (error.Code != 0)
                    {
                        var exception = new ScpiErrorException(error.Code, error.Message);
                        exception.Data["Instrument"] = alias;
                        if (!string.IsNullOrEmpty(context))
                            exception.Data["Context"] = context;
                        return exception;
                    }
                    return new ScpiErrorException(0, "No error");
                }
            }
        }


        public ScpiError QueryError()
        {
            var str = QueryNC<string>("SYSTem:ERRor?");
            return new ScpiError(str);
        }


        #region Query

        /// <summary>
        /// Query value of the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private T QueryNC<T>(string query, params object[] args)
        {
            using (ObtainLock())
            {
                var formattedQuery = FormatString(query, args);
                PrintNC(query, args);
                T res;
                try
                {
                    res = ReadNC<T>();
                }
                catch (Exception e)
                {
                    e.Data["Context"] = formattedQuery;
                    throw e;
                }
                return res;
            }
        }

        private T[] QueryNCArray<T>(string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                var res1 = QueryNC<string>(formattedQuery).Trim(new[] { '\r', '\n', '"', ' ' }).Split(',');
                // Workaround for Keysight returning EMPTY strings if there no items in list
                if (res1.Length == 1 && res1.First() == "EMPTY")
                    return new T[] { };
                var result = res1.Select(value =>
                (T)typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, value)
                ).ToArray();

                return result;
            }
        }

        private async Task<T> QueryNCAsync<T>(int? timeout, string query, params object[] args)
        {
            using (ObtainLock())
            {
                T res;
                var formattedQuery = FormatString(query, args);
                PrintNC(formattedQuery);
                try
                {
                    res = await ReadNCAsync<T>(timeout);
                }
                catch (Exception e)
                {
                    e.Data["Context"] = formattedQuery;
                    throw e;
                }
                return res;
            }
        }

        /// <summary>
        /// Asynronous query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<T> QueryAsync<T>(int? timeout, string query, params object[] args)
        {
            using (ObtainLock())
            {
                var formattedQuery = FormatString(query, args);
                PrintNC(formattedQuery);
                T res;
                try
                {
                    res = await ReadNCAsync<T>(timeout);
                }
                catch (Exception e)
                {
                    e.Data["Context"] = formattedQuery;
                    throw e;
                }
                return res;
            }
        }

        /// <summary>
        /// Asyncronous array query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<T[]> QueryNCArrayAsync<T>(int? timeout, string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                var resp = await QueryNCAsync<string>(timeout, formattedQuery);
                var res1 = resp.Trim(new[] { '\r', '\n', '"', ' ' }).Split(',');
                var result = res1.Select(value =>
                (T)typeConverter.ConvertFromString(null, CultureInfo.InvariantCulture, value)
                ).ToArray();

                return result;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="query"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task<T[]> QueryArrayAsync<T>(int? timeout, string query, params object[] args)
        {
            using (ObtainLock())
            {
                string formattedQuery = FormatString(query, args);
                var result = await QueryNCArrayAsync<T>(timeout, formattedQuery);
                ThrowExceptionOnError(formattedQuery);
                return result;
            }
        }

        #endregion // Query

        #region Read


        private T ReadNC<T>()
        {
            using (ObtainLock())
            {
#if VISACOM
            
            string result = session.ReadString(65546);
#else
                string result = Session.FormattedIO.ReadLine();
#endif
                LogCommandReceived(result);
                return ConvertFromString<T>(result);
            }
        }

        private Task<T> ReadNCAsync<T>(int? timeout = null)
        {
            using (ObtainLock())
            {
                int DefaultTimeout = 2000;
                if (timeout != null)
                {
#if VISACOM
                DefaultTimeout = session.Timeout;
                session.Timeout = timeout.Value;
#else
                    DefaultTimeout = Session.TimeoutMilliseconds;
                    Session.TimeoutMilliseconds = timeout.Value;
#endif
                }

#if VISACOM
            // Do things synchronously
            return Task.FromResult(ReadNC<T>(session));
#else
                var task = new TaskCompletionSource<T>();

                Session.RawIO.BeginRead(1000, res =>
                {
                    if (res.IsCompleted)
                    {
                        if (res.Count == 0)
                        {
                            //throw new Exception(string.Format("Asynchronous operation {0} timed out.", nameof(ReadNCAsync)));
                            Exception ex = new TimeoutException(string.Format("Asynchronous operation {0} timed out.", nameof(ReadNCAsync)));
                            task.SetException(ex);
                            //task.SetCanceled();
                        }
                        else
                        {
                            string str = System.Text.Encoding.Default.GetString(res.Buffer, 0, (int)res.Count);

                            TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));
                            LogCommandReceived(str);
                            task.SetResult(ConvertFromString<T>(str));
                        }
                    }
                    else
                    {
                        var Exception = new OperationCanceledException(string.Format("Asynchronous operation {0} not completed.", nameof(ReadNCAsync)));
                        task.SetException(Exception);
                    }
                    Session.RawIO.EndRead(res);
                    // Restore timeout
                    if (timeout != null)
                    {
                        Session.TimeoutMilliseconds = DefaultTimeout;
                    }
                }, null);
                return task.Task;
#endif
            }
        }
        #endregion // Read
    }
}
