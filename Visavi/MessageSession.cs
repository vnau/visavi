using System;

#region visacom
#if VISACOM
using Ivi.Visa.Interop;
using IMessageInterface = Ivi.Visa.Interop.IMessage;
#else
using Ivi.Visa;
using IMessageInterface = Ivi.Visa.IMessageBasedSession;
#endif
#endregion

namespace Visavi
{
    public class MessageSession : MessageSessionContext
    {
        public MessageSession(IMessageInterface messageInterface) : base()
        {
            SetMessageInterface(messageInterface);
        }

        public MessageSession(IVisaSession session) : base()
        {
            if (session is IMessageInterface messageInterface)
            {
                SetMessageInterface(messageInterface);
            }
            else
            {
                throw new Exception("provided session is not IMessageInterface");
            }
        }

    }
}
