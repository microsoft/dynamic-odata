// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Dynamic.OData.Exceptions
{
    [Serializable]
    public class InvalidPropertyException : Exception
    {
        private const string InvalidSelectProperty = "Invalid {0} clause - Property {1} does not exist in model";
        public InvalidPropertyException(string clauseName, string propertyName) : base(string.Format(InvalidSelectProperty, clauseName, propertyName))
        {

        }
        public InvalidPropertyException(string clauseName, string propertyName, Exception innerException)
            : base(string.Format(InvalidSelectProperty, clauseName, propertyName), innerException)
        {
        }
    }
}
