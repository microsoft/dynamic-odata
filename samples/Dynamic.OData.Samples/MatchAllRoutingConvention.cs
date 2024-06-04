// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Conventions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using static System.Collections.Specialized.BitVector32;


namespace Dynamic.OData.Samples
{
    public class MatchAllControllerActionRoutingConvention : IODataControllerActionConvention
    {
        private readonly string _prefix;
        public MatchAllControllerActionRoutingConvention(string prefix)
        {
            _prefix = prefix;
        }

        public int Order => -100;

        public bool AppliesToController(ODataControllerActionContext context)
        {
            return context.Controller.ControllerName == "MatchAll" || context.Controller.ControllerName == "Metadata";
        }

        public bool AppliesToAction(ODataControllerActionContext context)
        {
            ActionModel action = context.Action;
            string actionName = action.ActionName;
            if (actionName == "GetMetadata" && context.Controller.ControllerName == "Metadata")
            {
                ODataPathTemplate path = new ODataPathTemplate(new ODataEntityNameMetadataTemplate());
                action.AddSelector("get", _prefix, EdmCoreModel.Instance, path);
                return true;
            }

            if (actionName == "Get" && context.Controller.ControllerName == "MatchAll")
            {
                ODataPathTemplate path = new ODataPathTemplate(new ODataEntityNameTemplate());
                action.AddSelector("get", _prefix, EdmCoreModel.Instance, path);
                return true;
            }

            return true; // stop to execute other conventions
        }
    }

    public class ODataEntityNameTemplate : ODataSegmentTemplate
    {
        public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
        {
            yield return "{entityname}";
        }

        public override bool TryTranslate(ODataTemplateTranslateContext context)
        {
            if (!context.RouteValues.TryGetValue("entityname", out object entitysetNameObj))
            {
                return false;
            }

            string entitySetName = entitysetNameObj as string;

            // if you want to support case-insensitive
            var edmEntitySet = context.Model.EntityContainer.EntitySets()
                .FirstOrDefault(e => string.Equals(entitySetName, e.Name, StringComparison.OrdinalIgnoreCase));

            if (edmEntitySet == null)
            {
                throw new InvalidOperationException($"Cannot find the entity using {entitySetName}");
            }

            context.Segments.Add(new EntitySetSegment(edmEntitySet));
            return true;
        }
    }

    public class ODataEntityNameMetadataTemplate : ODataSegmentTemplate
    {
        public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
        {
            // The existing requirement is to build the "template" as ".../{entityname}/$metadata".
            yield return "{entityname}/$metadata";
        }

        public override bool TryTranslate(ODataTemplateTranslateContext context)
        {
            context.Segments.Add(MetadataSegment.Instance);
            return true;
        }
    }

    //public class MatchAllRoutingConvention : IODataRoutingConvention
    //{
    //    private readonly string _matchAllControllerName;
    //    private readonly string _recommendationAction = "actions";
    //    public MatchAllRoutingConvention(string matchAllControllerName)
    //    {
    //        _matchAllControllerName = matchAllControllerName;
    //    }

    //    public IEnumerable<ControllerActionDescriptor> SelectAction(RouteContext routeContext)
    //    {
    //        //If metadata, route to metadata controller.
    //        if (routeContext == null || routeContext.HttpContext.Request.Path.Value.Contains("metadata", StringComparison.OrdinalIgnoreCase))
    //            return new MetadataRoutingConvention().SelectAction(routeContext);

    //        // Get a IActionDescriptorCollectionProvider from the global service provider.
    //        IActionDescriptorCollectionProvider actionCollectionProvider =
    //            routeContext.HttpContext.RequestServices.GetRequiredService<IActionDescriptorCollectionProvider>();

    //        //Get all actions on the match-all controller
    //        IEnumerable<ControllerActionDescriptor> actionDescriptors = actionCollectionProvider
    //                .ActionDescriptors.Items.OfType<ControllerActionDescriptor>()
    //                .Where(c => c.ControllerName == _matchAllControllerName);

    //        string path = routeContext.HttpContext.Request.Path.Value;
    //        return actionDescriptors.Where(
    //                 c => string.Equals(c.ActionName, "Get", StringComparison.OrdinalIgnoreCase)).ToList();

    //    }
    //}
}
