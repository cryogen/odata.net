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

namespace Microsoft.Data.OData.Query
{
    using System;
    using System.Diagnostics;
#if !WINDOWS_PHONE && !SILVERLIGHT && !PORTABLELIB
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
#endif

    /// <summary>
    /// The exception that is thrown when path parsing detects an unrecognized or unresolvable token in a path (which servers should treat as a 404).
    /// </summary>
#if !WINDOWS_PHONE && !SILVERLIGHT && !PORTABLELIB
    [Serializable]
#endif
    [DebuggerDisplay("{Message}")]
    public sealed class ODataUnrecognizedPathException : ODataException
    {
        /// <summary>
        /// Initializes a new instance of the ODataUnrecognizedPathException class.
        /// </summary>
        /// <remarks>
        /// The Message property is initialized to a system-supplied message 
        /// that describes the error. This message takes into account the 
        /// current system culture. 
        /// </remarks>
        public ODataUnrecognizedPathException()
            : this((string)Strings.ODataUriParserException_GeneralError, (Exception)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ODataUnrecognizedPathException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        public ODataUnrecognizedPathException(string message)
            : this(message, (Exception)null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DataServiceException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public ODataUnrecognizedPathException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        
#if !WINDOWS_PHONE && !SILVERLIGHT && !PORTABLELIB
        /// <summary>Creates a new instance of the <see cref="T:Microsoft.Data.OData.ODataUnrecognizedPathException" /> class from the  specified SerializationInfo and StreamingContext instances.</summary>
        /// <param name="info"> A SerializationInfo containing the information required to serialize the new ODataUnrecognizedPathException. </param>
        /// <param name="context"> A StreamingContext containing the source of the serialized stream  associated with the new ODataUnrecognizedPathException. </param>
        [SuppressMessage("Microsoft.Design", "CA1047", Justification = "Follows serialization info pattern.")]
        [SuppressMessage("Microsoft.Design", "CA1032", Justification = "Follows serialization info pattern.")]
        private ODataUnrecognizedPathException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
