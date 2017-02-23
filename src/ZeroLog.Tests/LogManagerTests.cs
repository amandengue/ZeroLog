﻿using System;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using NUnit.Framework;

namespace ZeroLog.Tests
{
    [TestFixture]
    public class LogManagerTests
    {
        private MessageCountingTestAppender _messageCountingTestAppender;

        [SetUp]
        public void SetUpFixture()
        {
            _messageCountingTestAppender = new MessageCountingTestAppender();
            LogManager.Initialize(new List<IAppender> {_messageCountingTestAppender}, 10);
        }

        [TearDown]
        public void Teardown()
        {
            LogManager.Shutdown();
        }

        [Test]
        public void should_create_log()
        {
            var log = LogManager.GetLogger(typeof(LogManagerTests));

            Check.That(log).IsNotNull();
        }
        
        [Test, ExpectedException]
        public void should_prevent_initialising_already_initialised_log_manager()
        {
            LogManager.Initialize(new IAppender[0]);
        }

        [Test]
        public void should_return_noop_log_event_when_no_more_log_event_are_available()
        {
            var log = LogManager.GetLogger(typeof(LogManagerTests));

            var actualLogEvents = new List<ILogEvent>();
            for (var i = 0; i < 10; i++)
            {
                actualLogEvents.Add(log.Debug());
            }

            var noopEvent = log.Debug();

            Check.That(actualLogEvents.OfType<LogEvent>().Count()).Equals(actualLogEvents.Count);
            Check.That(noopEvent).IsInstanceOf<NoopLogEvent>();

            var signal = _messageCountingTestAppender.SetMessageCountTarget(actualLogEvents.Count);

            for (var i = 0; i < actualLogEvents.Count; i++)
            {
                var actualLogEvent = actualLogEvents[i];
                actualLogEvent.Append(i).Log();
            }

            signal.Wait(TimeSpan.FromMilliseconds(100));

            Check.That(log.Debug()).IsInstanceOf<LogEvent>();
        }
    }
}