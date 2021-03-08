// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Dynamic.OData.Exceptions
{
    [Serializable]
    public class FeatureNotSupportedException : Exception
    {
        private const string FeatureNotSupported = "Feature Not Supported: Invalid {0} clause - {1}";
        public FeatureNotSupportedException(string clauseName, string message) : base(string.Format(FeatureNotSupported, clauseName, message))
        {

        }
        public FeatureNotSupportedException(string clauseName, string message, Exception innerException)
            : base(string.Format(FeatureNotSupported, clauseName, message), innerException)
        {
        }
    }
}
