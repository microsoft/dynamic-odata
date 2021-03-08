// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Dynamic.OData.Models;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Dynamic.OData.Samples
{
    public class ODataModelSettingsProvider : IODataModelSettingsProvider
    {
        public EdmEntityTypeSettings GetEdmModelSettingsFromRequest(HttpRequest httpRequest)
        {
            var pathSegments = httpRequest.Path.Value.Split('/');
            string routeName = string.Empty;
            if (pathSegments.Length >= 4)
                routeName = pathSegments[3];

            //Read this dynamically from a blob/redis
            var entitySettingList = JsonConvert.DeserializeObject<APIResponseMappingObject>(File.ReadAllText(Directory.GetCurrentDirectory() + @"\Settings\EntityTypeSettings.json"));
            
            return entitySettingList.EdmEntityTypeSettings.FirstOrDefault(predicate => string.Equals(predicate.RouteName,routeName, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    public interface IODataModelSettingsProvider
    {
        EdmEntityTypeSettings GetEdmModelSettingsFromRequest(HttpRequest httpRequest);
    }
}
