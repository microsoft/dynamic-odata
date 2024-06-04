// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Dynamic.OData.Models;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;
using System.Collections.Generic;

namespace Dynamic.OData
{
    public class ParseContext
    {
        public IEnumerable<IEdmEntityObject> Result { get; set; }

        public EdmModel Model { get; set; }

        public IEnumerable<Dictionary<string, object>> QueryableSourceEntities { get; set; }

        public List<EdmEntityTypeSettings> EdmEntityTypeSettings { get; set; }
        public Dictionary<string, object> LatestStateDictionary { get; set; }
    }
}
