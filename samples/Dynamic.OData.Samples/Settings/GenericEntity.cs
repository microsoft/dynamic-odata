// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Dynamic.OData.Samples.Settings
{
    /// <summary>
    /// A generic entity representing specfic classes. Key is property name value if property value
    /// </summary>
    public class GenericEntity
    {
        public string EntityName { get; set; }
        public Dictionary<string, object> PropertyList { get; set; }
    }
}
