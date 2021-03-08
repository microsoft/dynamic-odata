// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Dynamic.OData.Samples
{
    public class MatchAllRoutingConvention : IODataRoutingConvention
    {
        private readonly string _matchAllControllerName;
        private readonly string _recommendationAction = "actions";
        public MatchAllRoutingConvention(string matchAllControllerName)
        {
            _matchAllControllerName = matchAllControllerName;
        }

        public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
        {
            //If metadata, route to metadata controller.
            if (routeContext == null || routeContext.HttpContext.Request.Path.Value.Contains("metadata", StringComparison.OrdinalIgnoreCase))
                return new MetadataRoutingConvention().SelectAction(routeContext);

            // Get a IActionDescriptorCollectionProvider from the global service provider.
            IActionDescriptorCollectionProvider actionCollectionProvider =
                routeContext.HttpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();

            //Get all actions on the match-all controller
            IEnumerable<ControllerActionDescriptor> actionDescriptors = actionCollectionProvider
                    .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
                    .Where(c => c.ControllerName == _matchAllControllerName);

            string path = routeContext.HttpContext.Request.Path.Value;
            return actionDescriptors.Where(
                     c => string.Equals(c.ActionName, "Get", StringComparison.OrdinalIgnoreCase)).ToList();

        }
    }
}
