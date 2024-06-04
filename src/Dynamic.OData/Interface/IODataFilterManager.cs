// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Value;
using System.Collections.Generic;

namespace Dynamic.OData.Interface
{
    public interface IODataFilterManager
    {
        IEnumerable<IEdmEntityObject> ApplyFilter(IEnumerable<IEdmEntityObject> sourceEntities, IEnumerable<Dictionary<string, object>> queryableSourceEntities, HttpRequest request);

        void SetPropertyValue(Dictionary<string, string> attributes, IEnumerable<KeyValuePair<string, string>> entityTypeDictionary, Dictionary<string, object> keyValues = null);

        void SetActionInfoValue(Dictionary<string, string> actionInfo, IEnumerable<KeyValuePair<string, string>> entityTypeDictionary, Dictionary<string, object> keyValues);
    }
}
