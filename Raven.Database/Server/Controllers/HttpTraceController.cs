﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Raven.Abstractions.Data;
using Raven.Database.Server.Connections;
using Raven.Database.Server.Security;
using Raven.Database.Server.WebApi.Attributes;

namespace Raven.Database.Server.Controllers
{
    public class HttpTraceController : RavenDbApiController
    {
        [HttpGet]
        [RavenRoute("traffic-watch/events")]
        [RavenRoute("databases/{databaseName}/traffic-watch/events")]
        public HttpResponseMessage HttpTrace()
        {
            var traceTransport = new HttpTracePushContent(this);
            traceTransport.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");

            if (DatabaseName != null)
            {
                RequestManager.RegisterResourceHttpTraceTransport(traceTransport, DatabaseName);
            }
            else
            {
                var oneTimetokenPrincipal =  User as MixedModeRequestAuthorizer.OneTimetokenPrincipal;
                if ((oneTimetokenPrincipal != null && oneTimetokenPrincipal.IsAdministratorInAnonymouseMode) ||
                    SystemConfiguration.AnonymousUserAccessMode == AnonymousUserAccessMode.Admin)
                {
                    RequestManager.RegisterServerHttpTraceTransport(traceTransport);
                }
                else
                {
                    return GetMessageWithObject(
                        new
                        {
                            Error = "Administrator user is required in order to trace the whole server"
                        },
                        HttpStatusCode.Forbidden);
                }
            }

            return new HttpResponseMessage { Content = traceTransport };
        }
    }
}
