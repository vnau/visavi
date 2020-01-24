using System;
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
    public interface IMessageSessionContext
    {
        IMessageSession Session { get; }
        IMessageSessionContext Log(Action<string, MessageType, string, string> action);
        IMessageSessionContext WithResourceName(string resourceName);
        IMessageSessionContext WithTimeout(int timeout);
        IMessageSessionContext WithErrorsCheck(bool enable = true);
        void Print(string format, params object[] args);
        T Read<T>();
        string Query(string format, params object[] args);
        T Query<T>(string format, params object[] args);
        IDisposable ObtainLock();
        void ThrowExceptionOnError(string Context = null, bool IgnoreWarnings = true);
        ScpiErrorException GetErrorException(string context = null);
        ScpiError QueryError();
        Task<T> QueryAsync<T>(int? timeout, string format, params object[] args);
    }
}
