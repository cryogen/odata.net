//   Copyright 2011 Microsoft Corporation
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

#if !INTERNAL_DROP || ODATALIB

namespace Microsoft.Data.OData.Json
{
    #region Namespaces
    using System;
    using Microsoft.Data.Edm;
    #endregion

    /// <summary>
    /// Shared JSON util code for ODataLib and Server.
    /// </summary>
    internal static class JsonSharedUtils
    {
        /// <summary>
        /// Determines if the given double is serialized as a string in JSON.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>true if the value should be written as a string, false if should be written as a JSON number.</returns>
        internal static bool IsDoubleValueSerializedAsString(double value)
        {
#if ODATALIB
            DebugUtils.CheckNoExternalCallers();
#endif
            return Double.IsInfinity(value) || Double.IsNaN(value);
        }

        /// <summary>
        /// Determines if the given primitive value is of a basic type where we can rely on just the JSON representation to convey type information.
        /// If so, we don't have to write the type name.
        /// </summary>
        /// <param name="primitiveValue">The primitive value in question.</param>
        /// <param name="valueTypeReference">The type of the primitive value.</param>
        /// <returns>true if the given primitive value is of a basic JSON type, false otherwise.</returns>
        internal static bool ValueTypeMatchesJsonType(ODataPrimitiveValue primitiveValue, IEdmPrimitiveTypeReference valueTypeReference)
        {
#if ODATALIB
            DebugUtils.CheckNoExternalCallers();
#endif

            switch (valueTypeReference.PrimitiveKind())
            {
                // If the value being serialized is of a basic type where we can rely on just the JSON representation to convey type information, then never write the type name.
                case EdmPrimitiveTypeKind.Int32:
                case EdmPrimitiveTypeKind.String:
                case EdmPrimitiveTypeKind.Boolean:
                    return true;

                case EdmPrimitiveTypeKind.Double:
                    double doubleValue = (double)primitiveValue.Value;

                    // If a double value is positive infinity, negative infinity, or NaN, we serialize the double as a string.
                    // Thus the reader can't infer the type from the JSON representation, and we must write the type name explicitly 
                    // (i.e., if the property is open or the property type is assumed to be unknown, as is the case when writing in full metadata mode).
                    return !IsDoubleValueSerializedAsString(doubleValue);
                
                default:
                    return false;
            }
        }
    }
}

#endif
