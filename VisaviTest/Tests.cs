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

        public void LogHandler(string alias, MessageType type, string message, string context)
        {

        }

        [Test]
        public void TestQueryString()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responces = new Queue<string>(new string[] { "any string", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responces.Dequeue);

            var session = new MessageSession(mock.Session).WithErrorsCheck();
            var value = session.Query("DATA?");

            Assert.AreEqual(value, "any string");
            Assert.Pass();
        }

        [Test]
        public void TestQueryDouble()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responces = new Queue<string>(new string[] { "123.45", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responces.Dequeue);

            var session = new MessageSession(mock.Session).WithErrorsCheck();
            double value = session.Query<double>("FREQ?");


            Assert.AreEqual(value, 123.45);
            Assert.Pass();
        }

        [Test]
        public void TestQueryError()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responces = new Queue<string>(new string[] { "123.45", "+111, Error message" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responces.Dequeue);

            var session = new MessageSession(mock.Session).WithErrorsCheck(false);
            var value = session.Query<double>("POW?");
            var err = session.QueryError();

            Assert.AreEqual(err.Code, 111);
            Assert.AreEqual(err.Message, "Error message");
            Assert.Pass();
        }

        [Test]
        public void TestQueryArray()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responces = new Queue<string>(new string[] { "5,3,1", "+0, No error" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responces.Dequeue);

            var session = new MessageSession(mock.Session);
            var array = session.QueryArray<double>("LIST?");

            Assert.AreEqual(array, new double[] { 5, 3, 1 });
            Assert.Pass();
        }

        [Test]
        public void TestErrorException()
        {
            var mock = new VisaviTest.VisaSessionMock();
            var responces = new Queue<string>(new string[] { "5", "-666, Error message" });
            mock.FormattedIO.Setup(x => x.ReadLine()).Returns(responces.Dequeue);

            var session = new MessageSession(mock.Session);
            Assert.Catch<ScpiErrorException>(() => session.Query<double>("POW?"), "Error message");

            Assert.Pass();
        }

    }
}
