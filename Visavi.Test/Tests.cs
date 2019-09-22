using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using Visavi;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestFormatString()
        {
            var parameters = new List<string>();
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "any string", "+0, No error" });
            mock.FormattedIO.Setup(x => x.WriteLine(It.IsAny<string>())).Callback<string>(param => parameters.Add(param));

            var session = new MessageSession(mock.Session);
            session.Print(":SOUR1:TRAC:DATA {0}; :OUTP {1}; VALUE {2}", new double[] { 0.5, 0.1, 7 }, true, 5);
            Assert.That(parameters, Is.EqualTo(new[] { ":SOUR1:TRAC:DATA 0.5,0.1,7; :OUTP 1; VALUE 5" }));
            Assert.Pass();
        }

        [Test]
        public void TestQueryString()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "any string", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session).WithErrorsCheck();
            var value = session.Query("DATA?");

            Assert.AreEqual(value, "any string");
            Assert.Pass();
        }

        [Test]
        public void TestQueryDouble()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "123.45", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session).WithErrorsCheck();
            double value = session.Query<double>("FREQ?");


            Assert.AreEqual(value, 123.45);
            Assert.Pass();
        }

        [Test]
        public void TestQueryError()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "123.45", "+111, Error message" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session);
            var value = session.Query<double>("POW?");
            var err = session.QueryError();

            Assert.AreEqual(err.Code, 111);
            Assert.AreEqual(err.Message, "Error message");
            Assert.Pass();
        }

        [Test]
        public void TestQueryArrayGeneric()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "5.5,3.1, 1.64", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session);
            var array = session.Query<double[]>("LIST?");
            Assert.AreEqual(array, new double[] { 5.5, 3.1, 1.64 });
            Assert.Pass();
        }

        [Test]
        public void TestPrintReadArrayGeneric()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "5.5,3.1, 1.64", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session);
            session.Print("LIST?");
            var array = session.Read<double[]>();
            Assert.AreEqual(array, new double[] { 5.5, 3.1, 1.64 });
            Assert.Pass();
        }

        [Test]
        public void TestErrorException()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "-321, Error message" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);

            var session = new MessageSession(mock.Session).WithResourceName("ANALYZER").WithErrorsCheck();
            var exception = Assert.Catch<ScpiErrorException>(() => session.Print("POW {0}", 10), "Error message");
            Assert.AreEqual(-321, exception.HResult);
            Assert.AreEqual("POW 10", exception.Context);
            Assert.AreEqual("ANALYZER", exception.ResourceName);
            var stackTrace = exception.StackTrace;
            Assert.IsNotNull(stackTrace);
            Assert.IsTrue(stackTrace.Contains(nameof(TestErrorException)));
            Assert.IsFalse(stackTrace.Contains(nameof(MessageSessionContext)));

            Assert.Pass();
        }

        public void LogHandler(string alias, MessageType type, string message, string context)
        {

        }

        [Test]
        public void TestLog()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responses = new Queue<string>(new string[] { "-123, Error message" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responses.Dequeue);
            mock.MessageBasedSession.Setup(x => x.ResourceName).Returns("TCPIP::instrument::INSTR");

            var session = new MessageSession(mock.Session).WithErrorsCheck().Log(LogHandler);
            var exception = Assert.Catch<ScpiErrorException>(() => session.Print("FREQ {0}", 100), "Error message");
            Assert.AreEqual(-123, exception.HResult);
            Assert.AreEqual("FREQ 100", exception.Context);

            Assert.Pass();
        }

    }
}
