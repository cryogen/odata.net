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
    using System.Linq;
    using Microsoft.Data.Edm;
    using Microsoft.Data.OData.Metadata;
    using Microsoft.Data.OData.Query.SemanticAst;
    using Microsoft.Data.OData.Query.SyntacticAst;
    using ODataErrorStrings = Microsoft.Data.OData.Strings;

    /// <summary>
    /// Class that knows how to bind unary operators.
    /// </summary>
    internal sealed class UnaryOperatorBinder
    {
        /// <summary>
        /// Method to use for binding the parent node, if needed.
        /// </summary>
        private readonly Func<QueryToken, QueryNode> bindMethod;

        /// <summary>
        /// Constructs a UnaryOperatorBinder with the given method to be used binding the parent token if needed.
        /// </summary>
        /// <param name="bindMethod">Method to use for binding the parent token, if needed.</param>
        internal UnaryOperatorBinder(Func<QueryToken, QueryNode> bindMethod)
        {
            DebugUtils.CheckNoExternalCallers();
            this.bindMethod = bindMethod;
        }

        /// <summary>
        /// Binds a unary operator token.
        /// </summary>
        /// <param name="unaryOperatorToken">The unary operator token to bind.</param>
        /// <returns>The bound unary operator token.</returns>
        internal QueryNode BindUnaryOperator(UnaryOperatorToken unaryOperatorToken)
        {
            DebugUtils.CheckNoExternalCallers();
            ExceptionUtils.CheckArgumentNotNull(unaryOperatorToken, "unaryOperatorToken");

            SingleValueNode operand = this.GetOperandFromToken(unaryOperatorToken);

            IEdmTypeReference typeReference = UnaryOperatorBinder.PromoteOperandType(operand, unaryOperatorToken.OperatorKind);
            Debug.Assert(typeReference == null || typeReference.IsODataPrimitiveTypeKind(), "Only primitive types should be able to get here.");
            operand = MetadataBindingUtils.ConvertToTypeIfNeeded(operand, typeReference);

            return new UnaryOperatorNode(unaryOperatorToken.OperatorKind, operand);
        }

        /// <summary>
        /// Get the promoted type reference of the operand
        /// </summary>
        /// <param name="operand">the operand</param>
        /// <param name="unaryOperatorKind">the operator kind</param>
        /// <returns>the type reference of the operand</returns>
        private static IEdmTypeReference PromoteOperandType(SingleValueNode operand, UnaryOperatorKind unaryOperatorKind)
        {
            IEdmTypeReference typeReference = operand.TypeReference;
            if (!TypePromotionUtils.PromoteOperandType(unaryOperatorKind, ref typeReference))
            {
                string typeName = operand.TypeReference == null ? "<null>" : operand.TypeReference.ODataFullName();
                throw new ODataException(ODataErrorStrings.MetadataBinder_IncompatibleOperandError(typeName, unaryOperatorKind));
            }

            return typeReference;
        }

        /// <summary>
        /// Retrieve SingleValueNode operand from given token.
        /// </summary>
        /// <param name="unaryOperatorToken">The token</param>
        /// <returns>the SingleValueNode operand</returns>
        private SingleValueNode GetOperandFromToken(UnaryOperatorToken unaryOperatorToken)
        {
            SingleValueNode operand = this.bindMethod(unaryOperatorToken.Operand) as SingleValueNode;
            if (operand == null)
            {
                throw new ODataException(ODataErrorStrings.MetadataBinder_UnaryOperatorOperandNotSingleValue(unaryOperatorToken.OperatorKind.ToString()));
            }

            return operand;
        }
    }
}
