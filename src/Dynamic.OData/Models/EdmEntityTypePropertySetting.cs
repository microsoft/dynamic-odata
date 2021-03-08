// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Dynamic.OData.Models
{
    public class EdmEntityTypePropertySetting
    {
        public string PropertyName { get; set; }
        public string PropertyType { get; set; }
        public bool? IsNullable { get; set; }

        public EdmPrimitiveTypeKind GetEdmPrimitiveTypeKind()
        {
            if (Equals(PropertyType, TypeHandlingConstants.String))
                return EdmPrimitiveTypeKind.String;
            if (Equals(PropertyType, TypeHandlingConstants.Int16))
                return EdmPrimitiveTypeKind.Int16;
            if (Equals(PropertyType, TypeHandlingConstants.Int32))
                return EdmPrimitiveTypeKind.Int32;
            if (Equals(PropertyType, TypeHandlingConstants.Int64))
                return EdmPrimitiveTypeKind.Int64;
            if (Equals(PropertyType, TypeHandlingConstants.Guid))
                return EdmPrimitiveTypeKind.Guid;
            if (Equals(PropertyType, TypeHandlingConstants.DateTime))
                return EdmPrimitiveTypeKind.DateTimeOffset;
            if (Equals(PropertyType, TypeHandlingConstants.Date))
                return EdmPrimitiveTypeKind.Date;
            if (Equals(PropertyType, TypeHandlingConstants.TimeOfDay))
                return EdmPrimitiveTypeKind.TimeOfDay;
            if (Equals(PropertyType, TypeHandlingConstants.Boolean))
                return EdmPrimitiveTypeKind.Boolean;
            if (Equals(PropertyType, TypeHandlingConstants.Double))
                return EdmPrimitiveTypeKind.Double;
            if (Equals(PropertyType, TypeHandlingConstants.Decimal))
                return EdmPrimitiveTypeKind.Decimal;
            return EdmPrimitiveTypeKind.None;
        }

        private bool Equals(string source, string target)
        {
            return string.Equals(source, target, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
