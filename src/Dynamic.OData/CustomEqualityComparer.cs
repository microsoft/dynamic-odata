// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Dynamic.OData
{
    public class CustomEqualityComparer : IEqualityComparer<Dictionary<string, object>>
    {
        public bool Equals(Dictionary<string, object> a, Dictionary<string, object> b)
        {
            if (a == b) { return true; }
            if (a == null || b == null || a.Count != b.Count) { return false; }
            return !a.Except(b).Any();
        }

        public int GetHashCode(Dictionary<string, object> a)
        {
            return a.ToString().ToLower().GetHashCode();
        }
    }
}
