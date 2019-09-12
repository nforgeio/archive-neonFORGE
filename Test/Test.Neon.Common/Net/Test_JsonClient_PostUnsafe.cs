﻿//-----------------------------------------------------------------------------
// FILE:	    Test_JsonClient_PostUnsafe.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Neon.Common;
using Neon.Net;
using Neon.Retry;
using Neon.Xunit;

using Xunit;

namespace TestCommon
{
    public partial class Test_JsonClient
    {
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync()
        {
            // Ensure that POST returning an explict type works.

            RequestDoc requestDoc = null;

            using (new MockHttpServer(baseUri,
                context =>
                {
                    var request  = context.Request;
                    var response = context.Response;

                    if (request.Method != "POST")
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return;
                    }

                    if (request.Path.ToString() != "/info")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    requestDoc = NeonHelper.JsonDeserialize<RequestDoc>(request.GetBodyText());

                    var output = new ReplyDoc()
                        {
                            Value1 = "Hello World!"
                        };

                    response.ContentType = "application/json";

                    response.Write(NeonHelper.JsonSerialize(output));
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    var doc = new RequestDoc()
                    {
                        Operation = "FOO",
                        Arg0      = "Hello",
                        Arg1      = "World"
                    };

                    var reply = (await jsonClient.PostUnsafeAsync(baseUri + "info", doc)).As<ReplyDoc>();

                    Assert.Equal("Hello World!", reply.Value1);

                    Assert.Equal("FOO", requestDoc.Operation);
                    Assert.Equal("Hello", requestDoc.Arg0);
                    Assert.Equal("World", requestDoc.Arg1);
                }
            };
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_NotJson()
        {
            // Ensure that POST returning a non-JSON content type returns a NULL document.

            RequestDoc requestDoc = null;

            using (new MockHttpServer(baseUri,
                context =>
                {
                    var request  = context.Request;
                    var response = context.Response;

                    if (request.Method != "POST")
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return;
                    }

                    if (request.Path.ToString() != "/info")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    requestDoc = NeonHelper.JsonDeserialize<RequestDoc>(request.GetBodyText());

                    var output = new ReplyDoc()
                        {
                            Value1 = "Hello World!"
                        };

                    response.ContentType = "application/not-json";

                    response.Write(NeonHelper.JsonSerialize(output));
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    var doc = new RequestDoc()
                    {
                        Operation = "FOO",
                        Arg0      = "Hello",
                        Arg1      = "World"
                    };

                    var reply = (await jsonClient.PostUnsafeAsync(baseUri + "info", doc)).As<ReplyDoc>();

                    Assert.Null(reply);

                    Assert.Equal("FOO", requestDoc.Operation);
                    Assert.Equal("Hello", requestDoc.Arg0);
                    Assert.Equal("World", requestDoc.Arg1);
                }
            };
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_Args()
        {
            // Ensure that POST with query arguments work.

            RequestDoc requestDoc = null;

            using (new MockHttpServer(baseUri,
                context =>
                {
                    var request  = context.Request;
                    var response = context.Response;

                    if (request.Method != "POST")
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return;
                    }

                    if (request.Path.ToString() != "/info")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    requestDoc = NeonHelper.JsonDeserialize<RequestDoc>(request.GetBodyText());

                    var output = new ReplyDoc()
                        {
                            Value1 = request.QueryGet("arg1"),
                            Value2 = request.QueryGet("arg2")
                        };

                    response.ContentType = "application/json";

                    response.Write(NeonHelper.JsonSerialize(output));
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    var doc = new RequestDoc()
                    {
                        Operation = "FOO",
                        Arg0      = "Hello",
                        Arg1      = "World"
                    };

                    var reply = (await jsonClient.PostUnsafeAsync(baseUri + "info?arg1=test1&arg2=test2", doc)).As<ReplyDoc>();

                    Assert.Equal("test1", reply.Value1);
                    Assert.Equal("test2", reply.Value2);

                    Assert.Equal("FOO", requestDoc.Operation);
                    Assert.Equal("Hello", requestDoc.Arg0);
                    Assert.Equal("World", requestDoc.Arg1);
                }
            };
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_Dynamic()
        {
            // Ensure that POST returning a dynamic works.

            RequestDoc requestDoc = null;

            using (new MockHttpServer(baseUri,
                context =>
                {
                    var request  = context.Request;
                    var response = context.Response;

                    if (request.Method != "POST")
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return;
                    }

                    if (request.Path.ToString() != "/info")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    requestDoc = NeonHelper.JsonDeserialize<RequestDoc>(request.GetBodyText());

                    var output = new ReplyDoc()
                        {
                            Value1 = "Hello World!"
                        };

                    response.ContentType = "application/json";

                    response.Write(NeonHelper.JsonSerialize(output));
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    dynamic doc = new ExpandoObject();

                    doc.Operation = "FOO";
                    doc.Arg0      = "Hello";
                    doc.Arg1      = "World";

                    var reply = (await jsonClient.PostUnsafeAsync(baseUri + "info", doc)).AsDynamic();

                    Assert.Equal("Hello World!", (string)reply.Value1);

                    Assert.Equal("FOO", requestDoc.Operation);
                    Assert.Equal("Hello", requestDoc.Arg0);
                    Assert.Equal("World", requestDoc.Arg1);
                }
            };
        }
 
        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_Dynamic_NotJson()
        {
            // Ensure that POST returning non-JSON returns a NULL dynamic document.

            RequestDoc requestDoc = null;

            using (new MockHttpServer(baseUri,
                context =>
                {
                    var request  = context.Request;
                    var response = context.Response;

                    if (request.Method != "POST")
                    {
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        return;
                    }

                    if (request.Path.ToString() != "/info")
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    requestDoc = NeonHelper.JsonDeserialize<RequestDoc>(request.GetBodyText());

                    var output = new ReplyDoc()
                        {
                            Value1 = "Hello World!"
                        };

                    response.ContentType = "application/not-json";

                    response.Write(NeonHelper.JsonSerialize(output));
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    dynamic doc = new ExpandoObject();

                    doc.Operation = "FOO";
                    doc.Arg0 = "Hello";
                    doc.Arg1 = "World";

                    var reply = (await jsonClient.PostUnsafeAsync(baseUri + "info", doc)).AsDynamic();

                    Assert.Null(reply);

                    Assert.Equal("FOO", requestDoc.Operation);
                    Assert.Equal("Hello", requestDoc.Arg0);
                    Assert.Equal("World", requestDoc.Arg1);
                }
            };
        }

        [Fact]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_Error()
        {
            // Ensure that POST returning a hard error works.

            using(new MockHttpServer(baseUri,
                context =>
                {
                    var response = context.Response;

                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }))
            {
                using (var jsonClient = new JsonClient())
                {
                    var doc = new RequestDoc()
                    {
                        Operation = "FOO",
                        Arg0      = "Hello",
                        Arg1      = "World"
                    };

                    var response = await jsonClient.PostUnsafeAsync(baseUri + "info", doc);

                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    Assert.False(response.IsSuccess);
                    Assert.Throws<HttpException>(() => response.EnsureSuccess());
                }
            };
        }

        [Fact(Skip = "TODO")]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_Retry()
        {
            // Ensure that POST will retry after soft errors.

            // $todo(jeff.lill): Simulate socket errors via HttpClient mocking.

            await Task.Delay(0);
        }

        [Fact(Skip = "TODO")]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_NoRetryNull()
        {
            // Ensure that POST won't retry if [retryPolicy=NULL]

            // $todo(jeff.lill): Simulate socket errors via HttpClient mocking.

            await Task.Delay(0);
        }

        [Fact(Skip = "TODO")]
        [Trait(TestCategory.CategoryTrait, TestCategory.NeonCommon)]
        public async Task PostUnsafeAsync_NoRetryExplicit()
        {
            // Ensure that POST won't retry if [retryPolicy=NoRetryPolicy]

            // $todo(jeff.lill): Simulate socket errors via HttpClient mocking.

            await Task.Delay(0);
        }
    }
}
