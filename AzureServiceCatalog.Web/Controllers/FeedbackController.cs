﻿using AzureServiceCatalog.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;

namespace AzureServiceCatalog.Web.Controllers
{
    public class FeedbackController : ApiController
    {
        NotificationProcessor notificationProcessor = new NotificationProcessor();
        // POST: api/Feedback
        public IHttpActionResult Post([FromBody]FeedbackViewModel model)
        {
            notificationProcessor.SendFeedbackNotification(model);
            return Ok();
        }

        public IHttpActionResult Get()
        {
            var fbViewModel = new FeedbackViewModel();
            fbViewModel.Name = ClaimsPrincipal.Current.Name();
            fbViewModel.Email = ClaimsPrincipal.Current.Upn();
            return Ok(fbViewModel);
        }
    }
}
