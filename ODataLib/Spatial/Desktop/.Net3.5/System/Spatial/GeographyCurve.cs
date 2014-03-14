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

namespace System.Spatial
{
#if WINDOWS_PHONE
    using System.Runtime.Serialization;
#endif

    /// <summary>Represents the curve of geography.</summary>
#if WINDOWS_PHONE
    [DataContract]
#endif
    public abstract class GeographyCurve : Geography
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Spatial.GeographyCurve" /> class.</summary>
        /// <param name="coordinateSystem">The coordinate system of this geography curve.</param>
        /// <param name="creator">The implementation that created this instance.</param>
        protected GeographyCurve(CoordinateSystem coordinateSystem, SpatialImplementation creator)
            : base(coordinateSystem, creator)
        {
        }
    }
}
