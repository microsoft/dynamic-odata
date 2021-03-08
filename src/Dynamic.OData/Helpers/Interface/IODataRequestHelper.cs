﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Dynamic.OData.Models;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;

namespace Dynamic.OData.Helpers.Interface
{
    public interface IODataRequestHelper
    {
        public List<EdmEntityTypeSettings> GetEdmEntityTypeSettings(HttpRequest httpRequest);
        IEdmCollectionType GetEdmCollectionType(HttpRequest httpRequest);

        IEdmEntityTypeReference GetEdmEntityTypeReference(HttpRequest httpRequest);

        EdmModel GetEdmModel(HttpRequest httpRequest);

        ODataPath GetODataPath(HttpRequest httpRequest);

        Uri GetODataRelativeUri(HttpRequest httpRequest);

        void SetRequestCount(Microsoft.OData.UriParser.ODataUriParser oDataUriParser, HttpRequest httpRequest, int count);
        string GetRouteName(HttpRequest httpRequest);
        IEdmModel GetEdmModel(HttpRequest request, EdmEntityTypeSettings edmEntityTypeSettings, string namespaceName);
        string ModifyResponse(string responseBody, bool removeODataTypeProperty, bool updateODataContextSuffix, string odataSuffix);
        QueryString EscapeQueryString(QueryString querystring);
    }
}
