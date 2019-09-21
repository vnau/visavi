using System;

#region VisaType
#if VISACOM
using Ivi.Visa.Interop;
using IMessageSession = Ivi.Visa.Interop.IMessage;
#else
using Ivi.Visa;
using IMessageSession = Ivi.Visa.IMessageBasedSession;
#endif
#endregion VisaType

namespace Visavi
{
    public class MessageSession : MessageSessionContext
    {
        public MessageSession(IVisaSession session) : base()
        {
            if (session is IMessageSession messageInterface)
            {
                SetMessageInterface(messageInterface);
            }
            else
            {
                throw new ArgumentException("provided session is not IMessageInterface", nameof(messageInterface));
            }
        }

    }
}
