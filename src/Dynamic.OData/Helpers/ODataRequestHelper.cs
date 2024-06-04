// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dynamic.OData.Helpers.Interface;
using Dynamic.OData.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Newtonsoft.Json.Linq;

namespace Dynamic.OData.Helpers
{
    public class ODataRequestHelper : IODataRequestHelper
    {
        private const string odataContextKey = "odataContext";
        public List<EdmEntityTypeSettings> GetEdmEntityTypeSettings(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.Settings;
        }
        public ODataPath GetODataPath(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.Path;
        }
        public QueryString EscapeQueryString(QueryString querystring)
        {
            var escapedQuery = Regex.Replace(querystring.Value, "(Custom.CountDistinct_|Custom.Count_)%27(.*?)%27(%20)+as", m => m.Groups[1].Value +
                                "%27" + Uri.EscapeDataString(Uri.UnescapeDataString(m.Groups[2].Value).Replace("'", "''")) + "%27%20as");
            return new QueryString(escapedQuery);
        }
        public IEdmCollectionType GetEdmCollectionType(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.EdmCollectionType;
        }

        public IEdmEntityTypeReference GetEdmEntityTypeReference(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.EdmEntityTypeReference;
        }

        public EdmModel GetEdmModel(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.EdmModel;
        }

        public Uri GetODataRelativeUri(HttpRequest httpRequest)
        {
            var splituri = httpRequest.GetUri().ToString().Split('/').Last();
            var requestUri = new Uri(splituri, UriKind.Relative);
            return requestUri;
        }

        public void SetRequestCount(ODataUriParser oDataUriParser, HttpRequest httpRequest, int count)
        {
            var isCountPredicatePresent = oDataUriParser.ParseCount();
            if (isCountPredicatePresent == true)
                httpRequest.ODataFeature().TotalCount = count;
        }

        public string GetRouteName(HttpRequest httpRequest)
        {
            var context = (ODataContext)httpRequest.HttpContext.Items[odataContextKey];
            return context.ActualRouteName;
        }

        public IEdmModel GetEdmModel(HttpRequest request, EdmEntityTypeSettings edmEntityTypeSettings, string namespaceName)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (edmEntityTypeSettings == null)
                throw new ArgumentNullException(nameof(edmEntityTypeSettings));

            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentNullException(nameof(namespaceName));

            string modelName = edmEntityTypeSettings.RouteName.ToLowerInvariant();

            var odataContext = new ODataContext();
            EdmModel model = new EdmModel();
            EdmEntityContainer container = new EdmEntityContainer(namespaceName, "container");
            model.AddElement(container);
            var edmEntityType = new EdmEntityType(namespaceName, modelName);

            foreach (var property in edmEntityTypeSettings.Properties)
            {
                if (property.PropertyName.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    if (property.IsNullable.HasValue)
                        edmEntityType.AddKeys(edmEntityType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind(), property.IsNullable.Value));
                    else
                        edmEntityType.AddKeys(edmEntityType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind()));
                }
                else
                {

                    if (property.IsNullable.HasValue)
                        edmEntityType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind(), property.IsNullable.Value);
                    else
                        edmEntityType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind());
                }
            }
            model.AddElement(edmEntityType);
            container.AddEntitySet(modelName, edmEntityType);

            //Set the context
            odataContext.Settings = new List<EdmEntityTypeSettings>
                {
                    edmEntityTypeSettings
                };
            odataContext.Path = new ODataPath();
            odataContext.EdmModel = model;
            odataContext.EdmEntityTypeReference = new EdmEntityTypeReference(edmEntityType, true);
            odataContext.EdmCollectionType = new EdmCollectionType(odataContext.EdmEntityTypeReference);
            odataContext.ActualRouteName = modelName;
            request.HttpContext.Items.Add(odataContextKey, odataContext);
            return model;
        }

        public string ModifyResponse(string responseBody, bool removeODataTypeProperty, bool updateODataContextSuffix, string odataSuffix)
        {
            var responseObject = JObject.Parse(responseBody);
            if(updateODataContextSuffix)
            {
                var oldContextValue = responseObject["@odata.context"].ToString();
                var newContextValue = oldContextValue.Split("#")[0] + "#" + odataSuffix;
                responseObject["@odata.context"] = newContextValue;
            }
            var type = responseObject["value"].Type;
        
            if (removeODataTypeProperty && !type.Equals(JTokenType.Boolean))
            {
                var itemArray = (JArray)responseObject["value"];
                foreach (var item in itemArray)
                {
                    var itemObj = (JObject)item;
                    itemObj.Remove("@odata.type");
                }
            }
            return responseObject.ToString(Newtonsoft.Json.Formatting.None);
        }

        //public IEdmModel GetEdmModel(HttpRequest request, List<EdmEntityTypeSettings> edmEntityTypeSettings, string namespaceName)
        //{
        //    if (request == null)
        //        throw new ArgumentNullException(nameof(request));

        //    if (edmEntityTypeSettings == null || edmEntityTypeSettings.Count == 0)
        //        throw new ArgumentNullException(nameof(edmEntityTypeSettings));

        //    if (string.IsNullOrWhiteSpace(namespaceName))
        //        throw new ArgumentNullException(nameof(namespaceName));

        //    var model = new EdmModel();
        //    var container = new EdmEntityContainer(namespaceName, "container");
        //    var odataContext = new ODataContext();
        //    string datasetname = "all";
        //    var masterEntityType = new EdmEntityType(namespaceName, datasetname);
        //    model.AddElement(masterEntityType);
        //    foreach (var kvp in edmEntityTypeSettings)
        //    {
        //        var edmComplexType = new EdmComplexType(namespaceName, kvp.RouteName);
        //        foreach (var property in kvp.Properties)
        //        {
        //            if (property.IsNullable.HasValue)
        //                edmComplexType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind(), property.IsNullable.Value);
        //            else
        //                edmComplexType.AddStructuralProperty(property.PropertyName, property.GetEdmPrimitiveTypeKind());
        //        }
        //        model.AddElement(edmComplexType);
        //        masterEntityType.AddStructuralProperty(kvp.RouteName + "s", new EdmCollectionTypeReference(new EdmCollectionType(new EdmComplexTypeReference(edmComplexType, true))));
        //    }
        //    var masterEntitySet = new EdmEntitySet(container, datasetname, masterEntityType);
        //    container.AddElement(masterEntitySet);

        //    //Set the context
        //    odataContext.Settings = edmEntityTypeSettings.ToList();
        //    odataContext.Path = new ODataPath();
        //    odataContext.EdmModel = model;
        //    odataContext.EdmEntityTypeReference = new EdmEntityTypeReference(masterEntityType, true);
        //    odataContext.EdmCollectionType = new EdmCollectionType(odataContext.EdmEntityTypeReference);
        //    odataContext.ActualRouteName = datasetname;
        //    request.HttpContext.Items.Add(odataContextKey, odataContext);
        //    return model;
        //}
    }

    public static class RequestExtensions
    {
        public static Uri GetUri(this HttpRequest request)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port.GetValueOrDefault(80),
                Path = request.Path.ToString(),
                Query = request.QueryString.ToString()
            };
            return uriBuilder.Uri;
        }
    }
}
