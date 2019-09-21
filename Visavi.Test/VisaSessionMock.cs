using Ivi.Visa;
using Moq;
using System;

namespace VisaviTest
{
    class VisaSessionMock
    {
        public Mock<IMessageBasedSession> MessageBasedSession { get; }
        public Mock<IMessageBasedFormattedIO> FormattedIO { get; }
        public IVisaSession Session { get; }
        public Mock<IMessageBasedRawIO> RawIO { get; }

        public VisaSessionMock()
        {
            MessageBasedSession = new Mock<IMessageBasedSession>();
            FormattedIO = new Mock<IMessageBasedFormattedIO>();
            RawIO = new Mock<IMessageBasedRawIO>();
            var dispose = MessageBasedSession.As<IDisposable>();
            dispose.Setup(x => x.Dispose()).Callback(() => { });
            Session = MessageBasedSession.As<IVisaSession>().Object;
            
            MessageBasedSession.Setup(a => a.FormattedIO).Returns(FormattedIO.Object);
            MessageBasedSession.Setup(a => a.RawIO).Returns(RawIO.Object);
        }
    }

}
