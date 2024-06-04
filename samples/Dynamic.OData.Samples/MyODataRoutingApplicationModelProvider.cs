// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;

namespace Dynamic.OData.Samples
{
    public class MyODataRoutingApplicationModelProvider : IApplicationModelProvider
    {
        public MyODataRoutingApplicationModelProvider(
            IOptions<ODataOptions> options)
        {
            options.Value.AddRouteComponents("odata/entities", EdmCoreModel.Instance);
        }

        /// <summary>
        /// Gets the order value for determining the order of execution of providers.
        /// </summary>
        public int Order => 90;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            EdmModel model = new EdmModel();
            const string prefix = "odata/entities";
            foreach (var controllerModel in context.Result.Controllers)
            {
                if (controllerModel.ControllerName == "MatchAll")
                {
                    ProcessHandleAll(prefix, model, controllerModel);
                    continue;
                }

                if (controllerModel.ControllerName == "Metadata")
                {
                    ProcessMetadata(prefix, model, controllerModel);
                    continue;
                }
            }
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
        }

        private void ProcessHandleAll(string prefix, IEdmModel model, ControllerModel controllerModel)
        {
            foreach (var actionModel in controllerModel.Actions)
            {
                if (actionModel.ActionName == "Get" && actionModel.Parameters.Count == 0)
                {
                    ODataPathTemplate path = new ODataPathTemplate(new ODataEntityNameTemplate());
                    actionModel.AddSelector("get", prefix, model, path);
                }
            }
        }

        private void ProcessMetadata(string prefix, IEdmModel model, ControllerModel controllerModel)
        {
            foreach (var actionModel in controllerModel.Actions)
            {
                if (actionModel.ActionName == "GetMetadata")
                {
                    ODataPathTemplate path = new ODataPathTemplate(new ODataEntityNameMetadataTemplate());
                    actionModel.AddSelector("get", prefix, model, path);
                }
            }
        }
    }
}
