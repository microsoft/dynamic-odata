﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Routing;
using Microsoft.OData.Edm;
using System.Collections.Generic;

namespace Dynamic.OData.Models
{
    public class ODataContext
    {
        public List<EdmEntityTypeSettings> Settings { get; set; }

        public ODataPath Path { get; set; }

        public EdmModel EdmModel { get; set; }

        public IEdmCollectionType EdmCollectionType { get; set; }

        public IEdmEntityTypeReference EdmEntityTypeReference { get; set; }

        public string ActualRouteName { get; set; }
    }
}