/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using MindTouch.Tasking;
using MindTouch.Web;
using MindTouch.Xml;

using NUnit.Framework;

namespace MindTouch.Dream.Test {

    [TestFixture]
    public class DreamHostTest {

        //--- Fields ---
        private Plug _host;
        private DreamHostInfo _hostinfo;

        //--- Methods ---

        [SetUp]
        public void Init() {
            _hostinfo = DreamTestHelper.CreateRandomPortHost(new XDoc("config").Elem("dream.in.authtoken", "abc"));
            _host = _hostinfo.LocalHost.At("host").With("apikey", _hostinfo.ApiKey);
        }

        [TearDown]
        public void DeinitTest() {
            System.GC.Collect();
            _hostinfo.Dispose();
        }

        [Test]
        public void Host_Blueprint_should_be_publicly_accessible() {
            Plug local = Plug.New(_host.Uri.WithoutQuery()).At("@blueprint");
            DreamMessage response = local.Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
        }

        [Test]
        public void TestHost() {
            Plug test = _host.At("test");
            DreamMessage response = test.Get();
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("", response.ToDocument()["body"].Contents);
        }

        [Test]
        public void TestHostOkEmpty() {
            Plug test = _host.At("test");
            DreamMessage response = test.Post(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("", response.ToDocument()["body"].Contents);
        }

        [Test]
        public void TestHostOkBody() {
            Plug test = _host.At("test");
            DreamMessage response = test.Post(DreamMessage.Ok(new XDoc("root").Start("tag").Value(42).End()));
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("<root><tag>42</tag></root>", response.ToDocument()["body"].Contents);
        }

        [Test]
        public void TestHostBadResponse() {
            Plug test = _host.At("test").With("status", (int)DreamStatus.BadRequest);
            DreamMessage response = test.Post(DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.BadRequest, response.Status);
        }

        [Test]
        public void TestHostNotFound() {
            Plug test = _host.At("test").With("status", (int)DreamStatus.NotFound);
            DreamMessage response = test.Post(DreamMessage.Ok(), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.NotFound, response.Status);
        }

        [Test]
        public void TestVerbOverride1() {
            Plug test = _host.At("test").With(DreamInParam.VERB, "PUT");
            DreamMessage response = test.Post(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("PUT", response.ToDocument()["verb"].Contents);
        }

        [Test]
        public void TestVerbOverride2() {
            Plug test = _host.At("test");
            DreamMessage request = DreamMessage.Ok();
            request.Headers["X-HTTP-Method-Override"] = "PUT";
            DreamMessage response = test.Post(request);
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("PUT", response.ToDocument()["verb"].Contents);
        }

        [Test]
        public void TestFunnyVerb() {
            Plug test = _host.At("test").With(DreamInParam.VERB, "FUNNY");
            DreamMessage response = test.Post(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("FUNNY", response.ToDocument()["verb"].Contents);
        }

        [Test]
        public void TestPlugHeader() {
            Plug test = _host.At("test");
            DreamMessage response = test.WithHeader("X-Test", "test-value").Post(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual("test-value", response.ToDocument()["headers/X-Test"].Contents);
        }

        [Test]
        public void TestCookie1() {
            Plug test = _host.At("test").With("cookie", "test-value");
            DreamMessage response = test.Post(DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual(1, response.Cookies.Count);
            Assert.AreEqual("test-cookie", response.Cookies[0].Name);
            Assert.AreEqual("test-value", response.Cookies[0].Value);
        }

        [Test]
        public void TestCookie2() {
            Plug test = _host.At("test");
            DreamMessage request = DreamMessage.Ok();
            DreamCookie cookie = new DreamCookie("test-cookie", "test-value", null);
            request.Cookies.Add(cookie);
            DreamMessage response = test.Post(request);
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsFalse(response.ToDocument()["body"].IsEmpty);
            Assert.AreEqual(cookie.ToString(), DreamCookie.ParseCookie(response.ToDocument()["headers/cookie"]).ToString());
        }

        [Test]
        [ExpectedException(typeof(DreamResponseException))]
        public void TestNotFoundFeature_Fail() {
            Plug test = _host.At("@about", "info");
            test.Get();
        }

        [Test]
        public void TestHostHeadOk() {
            Plug test = _host.At("test");
            DreamMessage response = test.Invoke("HEAD", DreamMessage.Ok());
            Assert.AreEqual(DreamStatus.Ok, response.Status);
            Assert.IsTrue((decimal)response.ContentLength > (decimal)0L);
            Assert.AreEqual(response.ToBytes().Length, 0);
        }

        [Test]
        public void XmlConvertIn() {
            string versit =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//MindTouch//Deki//Calendar//EN\r\n" +
                "VERSION:1.0\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART:20060902T063000Z\r\n" +
                "DTEND:20060902T071500Z\r\n" +
                "LOCATION;ENCODING=QUOTED-PRINTABLE:Joe's Office\r\n" +
                "UID:264665-2\r\n" +
                "CATEGORIES;ENCODING=QUOTED-PRINTABLE:Meeting\r\n" +
                "SUMMARY;ENCODING=QUOTED-PRINTABLE:Meeting: status & pizza\r\n" +
                "DESCRIPTION:Please show-up on time.\\n\\n\r\n" +
                " Pizzas will be ordered after meeting.\r\n" +
                "PRIORITY:2\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n";
            Plug test = _host.At("convert").With(DreamInParam.FORMAT, "versit").With(DreamInParam.ROOT, "icalendar");
            string response = test.Post(DreamMessage.Ok(MimeType.TEXT, versit)).ToText();
            string expected = "<icalendar><vcalendar><prodid>-//MindTouch//Deki//Calendar//EN</prodid><version>1.0</version><vevent><dtstart>2006-09-02T06:30:00Z</dtstart><dtend>2006-09-02T07:15:00Z</dtend><location encoding=\"QUOTED-PRINTABLE\">Joe's Office</location><uid>264665-2</uid><categories encoding=\"QUOTED-PRINTABLE\">Meeting</categories><summary encoding=\"QUOTED-PRINTABLE\">Meeting: status &amp; pizza</summary><description>Please show-up on time.\n\nPizzas will be ordered after meeting.</description><priority>2</priority></vevent></vcalendar></icalendar>";
            Assert.AreEqual(expected, response, "versit-xml did not match");
        }

        [Test]
        public void DecodeBase64In() {
            string base64 = "dGhpcyBpcyBhIGJhc2U2NCBkZWNvZGluZyB0ZXN0";
            Plug test = _host.At("convert").WithHeader(DreamHeaders.CONTENT_ENCODING, "base64");
            string response = test.Post(DreamMessage.Ok(MimeType.TEXT, base64)).ToText();
            string expected = "this is a base64 decoding test";
            Assert.AreEqual(expected, response, "decoded base64 did not match");
        }

        [Test]
        public void ServiceTest() {
            string sid = "http://services.mindtouch.com/dream/test/2007/03/sample";
            string sidInner = "http://services.mindtouch.com/dream/test/2007/03/sample-inner";
            _host.At("blueprints").Post(new XDoc("blueprint").Elem("assembly", "test.mindtouch.dream").Elem("class", "MindTouch.Dream.Test.SampleService"));
            _host.At("blueprints").Post(new XDoc("blueprint").Elem("assembly", "test.mindtouch.dream").Elem("class", "MindTouch.Dream.Test.SampleInnerService"));
            DreamServiceInfo info = DreamTestHelper.CreateService(_hostinfo,
                new XDoc("config")
                    .Elem("path", "sample")
                    .Elem("sid", sid)
                    .Start("prologue")
                        .Attr("name", "dummy")
                        .Value("p0")
                    .End()
                    .Start("epilogue")
                        .Attr("name", "dummy")
                        .Value("e0")
                    .End()
                    .Elem("apikey", "xyz"));
            Plug service = info.AtLocalHost.With("apikey", "xyz");

            // TODO (steveb):
            //  1) check that http://localhost:8081/host/services contains the newly started service
            //  2) check that http://localhost:8081/host/services contains the inner service of the newly started service
            //  3) check that http://localhost:8081/host/sample has the expected prologues/epilogues
            //  4) check that http://localhost:8081/host/sample/inner has the 'sample' as owner
            //  5) check that http://localhost:8081/host/sample/inner has the expected prologues/epilogues

            service.Delete();

            //  6) check that http://localhost:8081/host/services does not contain the started service
            //  7) check that http://localhost:8081/host/services does not contain the inner service

            _host.At("blueprints", XUri.DoubleEncodeSegment(sid)).Delete();
            _host.At("blueprints", XUri.DoubleEncodeSegment(sidInner)).Delete();
        }

        [Test]
        public void BadServiceTest() {
            string sid = "http://services.mindtouch.com/dream/test/2007/03/bad";
            _host.At("blueprints").Post(new XDoc("blueprint").Elem("assembly", "test.mindtouch.dream").Elem("class", "MindTouch.Dream.Test.SampleBadService"));
            DreamMessage response = _host.At("services").Post(new XDoc("config").Start("path").Value("sample").End().Start("sid").Value(sid).End(), new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.InternalError, response.Status);
        }

        [Test]
        [ExpectedException(typeof(DreamResponseException))]
        public void BadHostRequest() {
            Plug plug = Plug.New("http://nowhere/foo");
            plug.Get();
        }


        [Test]
        public void Can_get_host_timers_xml() {
            var response = _hostinfo.LocalHost.At("host", "status", "timers").With("apikey", _hostinfo.ApiKey).Get(new Result<DreamMessage>()).Wait();
            Assert.IsTrue(response.IsSuccessful);
        }

        [Test]
        public void RequestMessage_via_plug_is_closed_at_end_of_request() {
            var recipient = _hostinfo.CreateMockService();
            DreamMessage captured = null;
            recipient.Service.CatchAllCallback = (context, request, response) => {
                captured = request;
                response.Return(DreamMessage.Ok());
            };
            var requestMsg = DreamMessage.Ok(MimeType.TEXT, "foo");
            recipient.AtLocalHost.Post(requestMsg);
            Assert.IsNotNull(captured, "did not capture a message in mock service");
            Assert.IsTrue(captured.IsClosed, "captured message was not closed");
            Assert.IsTrue(requestMsg.IsClosed, "sent message is not closed");
        }

        [Test]
        public void RequestMessage_via_http_is_closed_at_end_of_request() {
            var recipient = _hostinfo.CreateMockService();
            DreamMessage captured = null;
            recipient.Service.CatchAllCallback = (context, request, response) => {
                captured = request;
                response.Return(DreamMessage.Ok());
            };
            var requestMsg = DreamMessage.Ok(MimeType.TEXT, "foo");
            var recipientUri = recipient.AtLocalHost.Uri.WithScheme("ext-http");
            Plug.New(recipientUri).Post(requestMsg);
            Assert.IsNotNull(captured, "did not capture a message in mock service");
            Assert.IsTrue(captured.IsClosed, "captured message was not closed");
            Assert.IsTrue(requestMsg.IsClosed, "sent message is not closed");
        }

        [Test]
        public void DreamIn_Origin_Host_and_Uri_are_stripped_if_DreamInAuth_header_is_not_set_on_host_requiring_DreamIn_Auth() {
            var recipient = _hostinfo.CreateMockService();
            XUri incomingUri = null;
            recipient.Service.CatchAllCallback = (context, request, response) => {
                incomingUri = context.Uri;
                response.Return(DreamMessage.Ok());
            };
            var recipientUri = recipient.AtLocalHost.Uri.WithScheme("ext-http");
            Plug.New(recipientUri)
                .With(DreamInParam.ROOT, "root")
                .With(DreamInParam.ORIGIN, "origin")
                .With(DreamInParam.HOST, "host")
                .With(DreamInParam.URI, "http://uri")
                .Get();
            Assert.IsNotNull(incomingUri);
            Assert.IsNull(incomingUri.GetParam(DreamInParam.ROOT));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.ORIGIN));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.HOST));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.URI));
        }

        [Test]
        public void DreamIn_Origin_Host_and_Uri_are_stripped_if_DreamInAuth_header_is_incorrect_on_host_requiring_DreamIn_Auth() {
            var recipient = _hostinfo.CreateMockService();
            XUri incomingUri = null;
            recipient.Service.CatchAllCallback = (context, request, response) => {
                incomingUri = context.Uri;
                response.Return(DreamMessage.Ok());
            };
            var recipientUri = recipient.AtLocalHost.Uri.WithScheme("ext-http");
            Plug.New(recipientUri)
                .With(DreamInParam.ROOT, "root")
                .With(DreamInParam.ORIGIN, "origin")
                .With(DreamInParam.HOST, "host")
                .With(DreamInParam.URI, "http://uri")
                .WithHeader(DreamHeaders.DREAM_IN_AUTH, "yyz")
                .Get();
            Assert.IsNotNull(incomingUri);
            Assert.IsNull(incomingUri.GetParam(DreamInParam.ROOT));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.ORIGIN));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.HOST));
            Assert.IsNull(incomingUri.GetParam(DreamInParam.URI));
        }

        [Test]
        public void DreamIn_Origin_Host_and_Uri_are_not_stripped_if_DreamInAuth_header_is_set_on_host_requiring_DreamIn_Auth() {
            var recipient = _hostinfo.CreateMockService();
            XUri incomingUri = null;
            recipient.Service.CatchAllCallback = (context, request, response) => {
                incomingUri = context.Uri;
                response.Return(DreamMessage.Ok());
            };
            var recipientUri = recipient.AtLocalHost.Uri.WithScheme("ext-http");
            Plug.New(recipientUri)
                .With(DreamInParam.ROOT, "root")
                .With(DreamInParam.ORIGIN, "origin")
                .With(DreamInParam.HOST, "host")
                .With(DreamInParam.URI, "http://uri")
                .WithHeader(DreamHeaders.DREAM_IN_AUTH, "abc")
                .Get();
            Assert.IsNotNull(incomingUri);
            Assert.AreEqual("root", incomingUri.GetParam(DreamInParam.ROOT));
            Assert.AreEqual("origin", incomingUri.GetParam(DreamInParam.ORIGIN));
            Assert.AreEqual("host", incomingUri.GetParam(DreamInParam.HOST));
            Assert.AreEqual("http://uri", incomingUri.GetParam(DreamInParam.URI));
        }
    }

    [TestFixture]
    public class DreamHostTests2 {

        // Note (arnec): these tests are separated from the above, since they require their own Dream host

        [Test]
        public void DreamIn_Origin_Host_and_Uri_are_not_stripped_if_the_host_doesn_not_require_DreamIn_Auth() {
            using(var hostinfo = DreamTestHelper.CreateRandomPortHost()) {
                var recipient = hostinfo.CreateMockService();
                XUri incomingUri = null;
                recipient.Service.CatchAllCallback = (context, request, response) => {
                    incomingUri = context.Uri;
                    response.Return(DreamMessage.Ok());
                };
                var recipientUri = recipient.AtLocalHost.Uri.WithScheme("ext-http");
                Plug.New(recipientUri)
                    .With(DreamInParam.ROOT, "root")
                    .With(DreamInParam.ORIGIN, "origin")
                    .With(DreamInParam.HOST, "host")
                    .With(DreamInParam.URI, "http://uri")
                    .Get();
                Assert.IsNotNull(incomingUri);
                Assert.AreEqual("root", incomingUri.GetParam(DreamInParam.ROOT));
                Assert.AreEqual("origin", incomingUri.GetParam(DreamInParam.ORIGIN));
                Assert.AreEqual("host", incomingUri.GetParam(DreamInParam.HOST));
                Assert.AreEqual("http://uri", incomingUri.GetParam(DreamInParam.URI));
            }
        }

    }
}