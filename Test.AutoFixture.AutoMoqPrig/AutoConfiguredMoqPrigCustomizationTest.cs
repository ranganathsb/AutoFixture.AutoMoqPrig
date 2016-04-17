/* 
 * File: AutoConfiguredMoqPrigCustomizationTest.cs
 * 
 * Author: Akira Sugiura (urasandesu@gmail.com)
 * 
 * 
 * Copyright (c) 2016 Akira Sugiura
 *  
 *  This software is MIT License.
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */


using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using System;
using System.Diagnostics;
using System.Diagnostics.Prig;
using System.Prig;
using System.Threading;
using System.Threading.Prig;
using Urasandesu.AutoFixture.AutoMoqPrig;
using Urasandesu.Moq.Prig;
using Urasandesu.Prig.Framework;

namespace Test.AutoFixture.AutoMoqPrig
{
    [TestFixture]
    public class AutoConfiguredMoqPrigCustomizationTest
    {
        const string AutoStringPattern = @"[\da-f]{8}-([\da-f]{4}-){3}[\da-f]{12}$"; // e.g. c0f98d5f-c6f7-48af-ac39-9e8217647cc2, Name949c7c83-c77b-434f-a8fe-e0aa73f81fbe

        [Test]
        public void AutoConfiguredMoqPrigCustomization_can_create_auto_mocked_prig_type()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Strict);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                fixture.Create<PEnvironment>();


                // Act
                var result = Environment.CurrentDirectory;


                // Assert
                Assert.That(result, Is.StringMatching(AutoStringPattern));
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_can_create_auto_mocked_instance_prig_type()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Strict);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                var curProc = fixture.Create<PProxyProcess>();
                PProcess.GetCurrentProcess().Body = () => curProc;


                // Act
                var result = Process.GetCurrentProcess().StartInfo.FileName;


                // Assert
                Assert.That(result, Is.StringMatching(AutoStringPattern));
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_can_create_complex_prig_type()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Strict);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                fixture.Create<PProcess>();


                // Act
                var result = Process.GetCurrentProcess().MainModule.FileName;


                // Assert
                Assert.That(result, Is.StringMatching(AutoStringPattern));
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_can_mock_out_parameter_method_automatically()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Default);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                fixture.Create<PInt32>();


                // Act
                var result = default(int);
                int.TryParse(int.MaxValue.ToString(), out result);


                // Assert
                Assert.AreNotEqual(0, result);
                Assert.AreNotEqual(int.MaxValue, result);
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_should_not_mock_generic_method_because_we_cannot_determine_the_generic_argument()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Default);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                PInterlocked.ExchangeOfTTRefT<Version>().Body = (ref Version location1, Version value) => new Version(1, 2, 3);
                fixture.Create<PInterlocked>();


                // Act
                var location = new Version(4, 5, 6);
                var result = Interlocked.Exchange(ref location, new Version(7, 8, 9));


                // Assert
                Assert.AreEqual(new Version(4, 5, 6), location);
                Assert.AreEqual(new Version(1, 2, 3), result);
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_should_not_mock_ref_parameter_method_because_it_is_used_for_returning_value_condition()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Default);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                PInterlocked.AddInt32RefInt32().Body = (ref int location1, int value) => 23;
                fixture.Create<PInterlocked>();


                // Act
                var location = 42;
                var result = Interlocked.Add(ref location, int.MaxValue);


                // Assert
                Assert.AreEqual(42, location);
                Assert.AreEqual(23, result);
            }
        }

        [Test]
        public void AutoConfiguredMoqPrigCustomization_should_not_mock_void_method_because_it_makes_no_effects()
        {
            using (new IndirectionsContext())
            {
                // Arrange
                var ms = new MockStorage(MockBehavior.Default);
                var fixture = new Fixture().Customize(new AutoConfiguredMoqPrigCustomization(ms));

                PConsole.WriteLineString().Body = value => { throw new NotImplementedException(); };
                fixture.Create<PConsole>();


                // Act, Assert
                Assert.Throws<NotImplementedException>(() => Console.WriteLine("bla bla bla"));
            }
        }
    }
}
