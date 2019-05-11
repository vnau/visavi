using System;
using System.Threading;

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
    public class SessionLocker : IDisposable
    {
        private IMessageInterface session;
        private bool lockResource;

        /// <summary>
        /// Lock session
        /// </summary>
        /// <param name="session">VISA session to lock</param>
        /// <param name="lockResource">Use VISA resource lock if true</param>
        public SessionLocker(IMessageInterface session, bool lockResource = false)
        {
            this.session = session;
            this.lockResource = lockResource;
            var nestedLock = Monitor.IsEntered(session);
            Monitor.Enter(session);
            if (lockResource && !nestedLock)
                session.LockResource();
        }

        /// <summary>
        /// Unlock session
        /// </summary>
        public void Dispose()
        {
            Monitor.Exit(session);
            var nestedLock = Monitor.IsEntered(session);
            if (lockResource && !nestedLock)
                session.UnlockResource();
        }
    }
}
