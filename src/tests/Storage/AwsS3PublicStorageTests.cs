﻿/*
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

using System;
using Autofac;
using Autofac.Builder;
using log4net;
using MindTouch.Aws;
using MindTouch.Dream.Test;
using MindTouch.Dream.Test.Aws;
using MindTouch.Extensions.Time;
using MindTouch.Tasking;
using MindTouch.Xml;
using Moq;
using NUnit.Framework;

namespace MindTouch.Dream.Storage.Test {
    [TestFixture]
    public class AwsS3PublicStorageTests {

        //--- Class Fields ---
        private static readonly ILog _log = LogUtils.CreateLog();

        //--- Fields ---
        private DreamHostInfo _hostInfo;
        private Mock<IAwsS3Client> _s3ClientMock;
        private AwsS3ClientProxy _clientProxy;
        private MockServiceInfo _mockService;
        private string _storageRoot;

        [TestFixtureSetUp]
        public void Init() {
            var root = "rootpath";
            var config = new XDoc("config")
                .Start("storage")
                    .Attr("type", "s3")
                    .Elem("root", root)
                    .Elem("bucket", "bucket")
                    .Elem("privatekey", "private")
                    .Elem("publickey", "public")
                .End();
            var builder = new ContainerBuilder();
            builder.Register((c, p) => {
                var s3Config = p.TypedAs<AwsS3ClientConfig>();
                Assert.AreEqual(root, s3Config.RootPath);
                Assert.AreEqual("default", s3Config.Endpoint.Name);
                Assert.AreEqual("bucket", s3Config.Bucket);
                Assert.AreEqual("private", s3Config.PrivateKey);
                Assert.AreEqual("public", s3Config.PublicKey);
                Assert.IsNull(_s3ClientMock, "storage already resolved");
                _clientProxy = new AwsS3ClientProxy();
                return _clientProxy;
            }).As<IAwsS3Client>().ServiceScoped();
            _hostInfo = DreamTestHelper.CreateRandomPortHost(config, builder.Build(ContainerBuildOptions.Default));
        }

        [SetUp]
        public void Setup() {
            _s3ClientMock = new Mock<IAwsS3Client>();
            _clientProxy.Client = _s3ClientMock.Object;
            _mockService = MockService.CreateMockService(_hostInfo);
            _storageRoot = _mockService.Service.Storage.Uri.LastSegment;
        }

        [Test]
        public void Can_init() {
            Assert.IsNotNull(_clientProxy);
        }

        [Test]
        public void Default_public_storage_root_cannot_be_read() {
            _mockService.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                var r = Plug.New((context.Service as MockService).Storage.Uri.WithoutLastSegment()).Get(new Result<DreamMessage>()).Wait();
                response2.Return(r);
            };
            var response = _mockService.AtLocalMachine.Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            _s3ClientMock.Verify(x => x.PutFile(It.IsAny<string>(), It.IsAny<AwsS3FileHandle>()), Times.Never());
        }

        [Test]
        public void Default_public_storage_root_cannot_be_written_to() {
            _mockService.Service.CatchAllCallback = delegate(DreamContext context, DreamMessage request, Result<DreamMessage> response2) {
                var r = Plug.New((context.Service as MockService).Storage.Uri.WithoutLastSegment())
                    .At("foo.txt")
                    .Put(DreamMessage.Ok(MimeType.TEXT, "bar"), new Result<DreamMessage>())
                    .Wait();
                response2.Return(r);
            };
            var response = _mockService.AtLocalMachine.Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
            _s3ClientMock.Verify(x => x.PutFile(It.IsAny<string>(), It.IsAny<AwsS3FileHandle>()), Times.Never());
        }

        [Test]
        public void Access_to_host_shared_private_service_should_be_forbidden() {
            var response = _hostInfo.LocalHost.At("host", "$store").Get(new Result<DreamMessage>()).Wait();
            Assert.AreEqual(DreamStatus.Forbidden, response.Status);
        }

        [Test]
        public void Can_put_file_without_expiration_at_path() {
            var data = StringUtil.CreateAlphaNumericKey(10);
            _s3ClientMock.Setup(x => x.PutFile(_storageRoot + "/foo/bar", It.Is<AwsS3FileHandle>(y => y.ValidateFileHandle(data, null)))).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").Put(DreamMessage.Ok(MimeType.TEXT, data), new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_put_file_without_expiration_at_root() {
            var data = StringUtil.CreateAlphaNumericKey(10);
            _s3ClientMock.Setup(x => x.PutFile(_storageRoot + "/foo", It.Is<AwsS3FileHandle>(y => y.ValidateFileHandle(data, null)))).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo").Put(DreamMessage.Ok(MimeType.TEXT, data), new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_put_file_with_expiration() {
            var data = StringUtil.CreateAlphaNumericKey(10);
            _s3ClientMock.Setup(x => x.PutFile(_storageRoot + "/foo/bar", It.Is<AwsS3FileHandle>(y => y.ValidateFileHandle(data, 10.Seconds())))).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").With("ttl", 10).Put(DreamMessage.Ok(MimeType.TEXT, data), new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Put_at_directory_path_fails() {
            var data = StringUtil.CreateAlphaNumericKey(10);
            _s3ClientMock.Setup(x => x.PutFile(_storageRoot + "/foo/bar/", It.IsAny<AwsS3FileHandle>())).Throws(new Exception("bad puppy")).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").WithTrailingSlash().Put(DreamMessage.Ok(MimeType.TEXT, data), new Result<DreamMessage>())
            );
            Assert.IsFalse(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_read_file() {
            var data = StringUtil.CreateAlphaNumericKey(10);
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar", false)).Returns(AwsTestHelpers.CreateFileInfo(data)).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").Get(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(data, response.ToText());
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_head_file() {
            var info = new AwsS3DataInfo(new AwsS3FileHandle() {
                Expiration = null,
                TimeToLive = null,
                MimeType = MimeType.TEXT,
                Modified = DateTime.UtcNow,
                Size = 10,
            });
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar", true)).Returns(info).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").Head(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(10, response.ContentLength);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Reading_nonexisting_file_returns_Not_Found() {
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar", false)).Returns((AwsS3DataInfo)null).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
                storage.At("foo", "bar").Get(new Result<DreamMessage>())
            );
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotFound, response.Status);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_list_directory() {
            var doc = new XDoc("files").Elem("x", StringUtil.CreateAlphaNumericKey(10));
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar/", false)).Returns(new AwsS3DataInfo(doc)).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
               storage.At("foo", "bar").WithTrailingSlash().Get(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            Assert.AreEqual(doc.ToCompactString(), response.ToDocument().ToCompactString());
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_head_directory() {
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar/", true)).Returns(new AwsS3DataInfo(new XDoc("x"))).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
              storage.At("foo", "bar").WithTrailingSlash().Head(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Listing_nonexisting_directory_returns_Not_Found() {
            _s3ClientMock.Setup(x => x.GetDataInfo(_storageRoot + "/foo/bar/", false)).Returns((AwsS3DataInfo)null).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
              storage.At("foo", "bar").WithTrailingSlash().Get(new Result<DreamMessage>())
            );
            Assert.IsFalse(response.IsSuccessful);
            Assert.AreEqual(DreamStatus.NotFound, response.Status);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_delete_file() {
            _s3ClientMock.Setup(x => x.Delete(_storageRoot + "/foo/bar")).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
              storage.At("foo", "bar").Delete(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }

        [Test]
        public void Can_delete_directory() {
            _s3ClientMock.Setup(x => x.Delete(_storageRoot + "/foo/bar/")).AtMostOnce().Verifiable();
            var response = _mockService.CallStorage(storage =>
              storage.At("foo", "bar").WithTrailingSlash().Delete(new Result<DreamMessage>())
            );
            Assert.IsTrue(response.IsSuccessful);
            _s3ClientMock.VerifyAll();
        }
    }
}
