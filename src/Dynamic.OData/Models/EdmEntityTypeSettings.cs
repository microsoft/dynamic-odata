// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Dynamic.OData.Models
{
    public class EdmEntityTypeSettings
    {
        /// <summary>
        /// List of Personas that have access on this route
        /// </summary>
        public List<string> Personas { get; set; }

        /// <summary>
        /// Route name would map to the family of the insight
        /// </summary>
        public string RouteName { get; set; }

        /// <summary>
        /// Properties which can be specified by the client to query by, outside of OData
        /// </summary>
        public List<string> QueryByAttributes { get; set; }

        /// <summary>
        /// List of property setting objects
        /// </summary>
        public List<EdmEntityTypePropertySetting> Properties { get; set; }
    }
}
