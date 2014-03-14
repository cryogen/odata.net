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
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Data.OData.Query.SemanticAst;
    using ODataErrorStrings = Microsoft.Data.OData.Strings;

    /// <summary>
    /// A component for walking an expand tree and determining if the depth or number of items exceed user-specified limits.
    /// </summary>
    internal sealed class ExpandDepthAndCountValidator
    {
        /// <summary>
        /// The maximum depth of any expand tree being validated.
        /// </summary>
        private readonly int maxDepth;

        /// <summary>
        /// The maximum number of expand items allowed in any expand tree being validated, including leaf and non-leaf nodes.
        /// </summary>
        private readonly int maxCount;

        /// <summary>
        /// The current count when validating a particular tree.
        /// </summary>
        private int currentCount;

        /// <summary>
        /// Initializes a new instance of <see cref="ExpandDepthAndCountValidator"/>.
        /// </summary>
        /// <param name="maxDepth">The maximum depth of an expand tree.</param>
        /// <param name="maxCount">The maximum number of expanded items allowed in a tree.</param>
        internal ExpandDepthAndCountValidator(int maxDepth, int maxCount)
        {
            DebugUtils.CheckNoExternalCallers();
            Debug.Assert(maxDepth >= 0, "Max depth cannot be negative.");
            Debug.Assert(maxCount >= 0, "Max count cannot be negative.");
            this.maxDepth = maxDepth;
            this.maxCount = maxCount;
        }

        /// <summary>
        /// Validates the given tree against the user-specified limits.
        /// </summary>
        /// <param name="expandTree">The expand tree to validate.</param>
        internal void Validate(SelectExpandClause expandTree)
        {
            DebugUtils.CheckNoExternalCallers();
            this.currentCount = 0;
            this.EnsureMaximumCountAndDepthAreNotExceeded(expandTree, /*currentDepth*/ 0);
        }

        /// <summary>
        /// Recursively ensures that the maximum count/depth are not exceeded by walking the tree.
        /// </summary>
        /// <param name="expandTree">The expand tree to walk and validate.</param>
        /// <param name="currentDepth">The current depth of the tree walk.</param>
        private void EnsureMaximumCountAndDepthAreNotExceeded(SelectExpandClause expandTree, int currentDepth)
        {
            Debug.Assert(expandTree != null, "expandTree != null");
            if (currentDepth > this.maxDepth)
            {
                throw ExceptionUtil.CreateBadRequestError(ODataErrorStrings.UriParser_ExpandDepthExceeded(currentDepth, this.maxDepth));
            }

            foreach (var expandItem in expandTree.SelectedItems.OfType<ExpandedNavigationSelectItem>())
            {
                this.currentCount++;
                if (this.currentCount > this.maxCount)
                {
                    throw ExceptionUtil.CreateBadRequestError(ODataErrorStrings.UriParser_ExpandCountExceeded(this.currentCount, this.maxCount));
                }

                this.EnsureMaximumCountAndDepthAreNotExceeded(expandItem.SelectAndExpand, currentDepth + 1);
            }
        }
    }
}
