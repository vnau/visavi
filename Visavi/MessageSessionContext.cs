﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;

#region VisaType
#if VISACOM
using Ivi.Visa.Interop;
using IMessageSession = Ivi.Visa.Interop.IMessage;
#else
using IMessageSession = Ivi.Visa.IMessageBasedSession;
#endif
#endregion VisaType


namespace Visavi
{
    public enum MessageType
    {
        Send,
        Receive,
        Warning
    }

    public class MessageSessionContext : IMessageSessionContext
    {
        private string resourceName;
        private int? timeout;
        private MessageSessionContext baseContext;
        private bool checkScpiError;
        private bool ignoreWarnings;
        private Action<string, MessageType, string, string> action;
        private IMessageSession messageSession;

        protected void SetMessageInterface(IMessageSession messageSession)
        {
            this.messageSession = messageSession;
        }

        /// <summary>
        /// Defaut context constructor
        /// </summary>
        protected MessageSessionContext()
        {
            timeout = 10000;
            checkScpiError = false;
            ignoreWarnings = true;
        }

        protected MessageSessionContext(MessageSessionContext context)
        {
            resourceName = context.resourceName;
            timeout = context.timeout;
            messageSession = context.messageSession;
            baseContext = context;
            checkScpiError = context.checkScpiError;
            ignoreWarnings = context.ignoreWarnings;
        }

        virtual public IMessageSession Session => messageSession;

        public IMessageSessionContext Log(Action<string, MessageType, string, string> action)
        {
            var context = new MessageSessionContext(this);
            context.action += action;
            return context;
        }

        /// <summary>
        /// Set alias for message session
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns>Message session context</returns>
        public IMessageSessionContext WithResourceName(string resourceName)
        {
            var context = new MessageSessionContext(this);
            context.resourceName = resourceName;
            return context;
        }

        /// <summary>
        /// Set read timeout for session
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public IMessageSessionContext WithTimeout(int timeout)
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
        public IMessageSessionContext WithErrorsCheck(bool enable = true)
        {
            var context = new MessageSessionContext(this);
            context.checkScpiError = enable;
            return context;
        }

        public void Print(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string query = FormatString(format, args);
                PrintNC(query);
                if (checkScpiError)
                    ThrowExceptionOnError(query);
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
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string Query(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string query = FormatString(format, args);
                var res = QueryNC<string>(query);
                if (checkScpiError)
                    ThrowExceptionOnError(query);
                return res;
            }
        }

        /// <summary>
        /// Query specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Query<T>(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string query = FormatString(format, args);
                var res = QueryNC<T>(query);
                if (checkScpiError)
                    ThrowExceptionOnError(query);
                return res;
            }
        }

        private void LogCommandReceived(string command)
        {
            action?.Invoke(resourceName, MessageType.Receive, command, null);
        }

        private void LogCommandSent(string command)
        {
            action?.Invoke(resourceName, MessageType.Send, command, null);
        }

        private void LogWarning(string message, string context)
        {
            action?.Invoke(resourceName, MessageType.Warning, message, context);
        }


        private void PrintNC(string format, params object[] args)
        {
            using (ObtainLock())
            {
                string query = FormatString(format, args);
                LogCommandSent(query);

#if VISACOM
                session.WriteString(formatted);
#else
                Session.FormattedIO.WriteLine(query);
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
        /// Format string with invariant culture (decimal separator is dot)
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns>formatted string</returns>
        private static string FormatString(string format, params object[] args)
        {
            return string.Format(new ScpiFormatProvider(), format, args);
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
                    var instrument = exception.ResourceName;
                    var context = exception.Context;
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
                        return new ScpiErrorException(error.Code, error.Message, resourceName, context);
                    }
                    return new ScpiErrorException(0, "No error", resourceName, context);
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
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private T QueryNC<T>(string format, params object[] args)
        {
            using (ObtainLock())
            {
                var query = FormatString(format, args);
                PrintNC(format, args);
                try
                {
                    return ReadNC<T>();
                }
                catch (ScpiErrorException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    e.Data["Context"] = query;
                    throw e;
                }
            }
        }

        private async Task<T> QueryNCAsync<T>(int? timeout, string format, params object[] args)
        {
            using (ObtainLock())
            {
                T res;
                var query = FormatString(format, args);
                PrintNC(query);
                try
                {
                    res = await ReadNCAsync<T>(timeout);
                }
                catch (Exception e)
                {
                    e.Data["Context"] = query;
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
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<T> QueryAsync<T>(int? timeout, string format, params object[] args)
        {
            using (ObtainLock())
            {
                var query = FormatString(format, args);
                PrintNC(query);
                T res;
                try
                {
                    res = await ReadNCAsync<T>(timeout);
                }
                catch (Exception e)
                {
                    e.Data["Context"] = query;
                    throw e;
                }
                return res;
            }
        }


        #endregion // Query

        #region Read


        private T ReadNC<T>()
        {
            using (ObtainLock())
            {
#if VISACOM
            
            string response = session.ReadString(65546);
#else
                string response = Session.FormattedIO.ReadLine();
#endif
                // remove trailing termination character if enabled
                if (Session.TerminationCharacterEnabled && !string.IsNullOrEmpty(response) && response[response.Length - 1] == Session.TerminationCharacter)
                    response = response.Substring(0, response.Length - 1);
                LogCommandReceived(response);
                return ScpiResponseConverter.ConvertFromString<T>(response);
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
                             task.SetResult(ScpiResponseConverter.ConvertFromString<T>(str));
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
