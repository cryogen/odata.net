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
    using Microsoft.Data.Edm;
    using Microsoft.Data.OData.Query.SemanticAst;
    using Microsoft.Data.OData.Query.SyntacticAst;
    using ODataErrorStrings = Microsoft.Data.OData.Strings;

    /// <summary>
    /// Class that knows how to bind a LambdaToken.
    /// </summary>
    internal sealed class LambdaBinder
    {
        /// <summary>
        /// Method used to bind a parent token.
        /// </summary>
        private readonly MetadataBinder.QueryTokenVisitor bindMethod;

        /// <summary>
        /// Constructs a LambdaBinder.
        /// </summary>
        /// <param name="bindMethod">Method used to bind a parent token.</param>
        internal LambdaBinder(MetadataBinder.QueryTokenVisitor bindMethod)
        {
            DebugUtils.CheckNoExternalCallers(); 
            ExceptionUtils.CheckArgumentNotNull(bindMethod, "bindMethod");
            this.bindMethod = bindMethod;
        }

        /// <summary>
        /// Binds a LambdaToken to metadata.
        /// </summary>
        /// <param name="lambdaToken">Token to bind.</param>
        /// <param name="state">Object to hold the state of binding.</param>
        /// <returns>A metadata bound any or all node.</returns>
        internal LambdaNode BindLambdaToken(LambdaToken lambdaToken, BindingState state)
        {
            DebugUtils.CheckNoExternalCallers(); 
            ExceptionUtils.CheckArgumentNotNull(lambdaToken, "LambdaToken");
            ExceptionUtils.CheckArgumentNotNull(state, "state");

            // Start by binding the parent token
            CollectionNode parent = this.BindParentToken(lambdaToken.Parent);
            RangeVariable rangeVariable = null;

            // Add the lambda variable to the stack
            if (lambdaToken.Parameter != null)
            {
                rangeVariable = NodeFactory.CreateParameterNode(lambdaToken.Parameter, parent);
                state.RangeVariables.Push(rangeVariable);
            }

            // Bind the expression
            SingleValueNode expression = this.BindExpressionToken(lambdaToken.Expression);
            
            // Create the node
            LambdaNode lambdaNode = NodeFactory.CreateLambdaNode(state, parent, expression, rangeVariable, lambdaToken.Kind);

            // Remove the lambda variable as it is now out of scope
            if (rangeVariable != null)
            {
                state.RangeVariables.Pop();
            }

            return lambdaNode;
        }

        /// <summary>
        /// Bind the parent of the LambdaToken
        /// </summary>
        /// <param name="queryToken">the parent token</param>
        /// <returns>the bound parent node</returns>
        private CollectionNode BindParentToken(QueryToken queryToken)
        {
            QueryNode parentNode = this.bindMethod(queryToken);
            CollectionNode parentCollectionNode = parentNode as CollectionNode;
            if (parentCollectionNode == null)
            {
                SingleValueOpenPropertyAccessNode parentOpenPropertyNode =
                    parentNode as SingleValueOpenPropertyAccessNode;

                if (parentOpenPropertyNode == null)
                {
                    throw new ODataException(ODataErrorStrings.MetadataBinder_LambdaParentMustBeCollection);
                }

                // TODO: Add support for open collection properties here by replacing
                //      with something like an OpenCollectionNode.
                throw new ODataException(ODataErrorStrings.MetadataBinder_CollectionOpenPropertiesNotSupportedInThisRelease);
            }

            return parentCollectionNode;
        }

        /// <summary>
        /// Bind the expression of the LambdaToken
        /// </summary>
        /// <param name="queryToken">the expression token</param>
        /// <returns>the bound expression node</returns>
        private SingleValueNode BindExpressionToken(QueryToken queryToken)
        {
            SingleValueNode expression = this.bindMethod(queryToken) as SingleValueNode;
            if (expression == null)
            {
                throw new ODataException(ODataErrorStrings.MetadataBinder_AnyAllExpressionNotSingleValue);
            }

            // type reference is allowed to be null for open properties.
            IEdmTypeReference expressionTypeReference = expression.GetEdmTypeReference();
            if (expressionTypeReference != null && !expressionTypeReference.AsPrimitive().IsBoolean())
            {
                throw new ODataException(ODataErrorStrings.MetadataBinder_AnyAllExpressionNotSingleValue);
            }

            return expression;
        }
    }
}
