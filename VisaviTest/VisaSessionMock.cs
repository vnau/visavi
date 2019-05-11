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

        public VisaSessionMock()
        {
            MessageBasedSession = new Mock<IMessageBasedSession>();
            FormattedIO = new Mock<IMessageBasedFormattedIO>();
            var dispose = MessageBasedSession.As<IDisposable>();
            dispose.Setup(x => x.Dispose()).Callback(() => { });
            Session = MessageBasedSession.As<IVisaSession>().Object;

            MessageBasedSession.Setup(a => a.FormattedIO).Returns(FormattedIO.Object);
        }
    }

}
