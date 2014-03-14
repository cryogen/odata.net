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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Data.Edm.Csdl.Internal.CsdlSemantics;
using Microsoft.Data.Edm.Csdl.Internal.Parsing;
using Microsoft.Data.Edm.Csdl.Internal.Parsing.Ast;
using Microsoft.Data.Edm.Validation;

namespace Microsoft.Data.Edm.Csdl
{
    /// <summary>
    /// Provides EDMX parsing services for EDM models.
    /// </summary>
    public class EdmxReader
    {
        private static readonly Dictionary<string, Action> EmptyParserLookup = new Dictionary<string, Action>();
        private readonly Dictionary<string, Action> edmxParserLookup;
        private readonly Dictionary<string, Action> runtimeParserLookup;
        private readonly Dictionary<string, Action> conceptualModelsParserLookup;
        private readonly Dictionary<string, Action> dataServicesParserLookup;
        private readonly XmlReader reader;
        private readonly List<EdmError> errors;
        private readonly CsdlParser csdlParser;
        private Version dataServiceVersion;
        private Version maxDataServiceVersion;

        /// <summary>
        /// True when either Runtime or DataServices node have been processed.
        /// </summary>
        private bool targetParsed;

        private EdmxReader(XmlReader reader)
        {
            this.reader = reader;
            this.errors = new List<EdmError>();
            this.csdlParser = new CsdlParser();

            // Setup the edmx parser.
            this.edmxParserLookup = new Dictionary<string, Action>
            {
                { CsdlConstants.Element_DataServices, this.ParseDataServicesElement },
                { CsdlConstants.Element_Runtime, this.ParseRuntimeElement }
            };
            this.dataServicesParserLookup = new Dictionary<string, Action>
            {
                { CsdlConstants.Element_Schema, this.ParseCsdlSchemaElement }
            };
            this.runtimeParserLookup = new Dictionary<string, Action>
            {
                { CsdlConstants.Element_ConceptualModels, this.ParseConceptualModelsElement }
            };
            this.conceptualModelsParserLookup = new Dictionary<string, Action>
            {
                { CsdlConstants.Element_Schema, this.ParseCsdlSchemaElement }
            };
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <param name="model">The model generated by parsing</param>
        /// <param name="errors">Errors reported while parsing.</param>
        /// <returns>Success of the parse operation.</returns>
        public static bool TryParse(XmlReader reader, out IEdmModel model, out IEnumerable<EdmError> errors)
        {
            EdmxReader edmxReader = new EdmxReader(reader);
            return edmxReader.TryParse(Enumerable.Empty<IEdmModel>(), out model, out errors);
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <returns>The model generated by parsing.</returns>
        public static IEdmModel Parse(XmlReader reader)
        {
            IEdmModel model;
            IEnumerable<EdmError> parseErrors;
            if (!TryParse(reader, out model, out parseErrors))
            {
                throw new EdmParseException(parseErrors);
            }

            return model;
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <param name="reference">Model to be referenced by the created model.</param>
        /// <param name="model">The model generated by parsing</param>
        /// <param name="errors">Errors reported while parsing.</param>
        /// <returns>Success of the parse operation.</returns>
        public static bool TryParse(XmlReader reader, IEdmModel reference, out IEdmModel model, out IEnumerable<EdmError> errors)
        {
            EdmxReader edmxReader = new EdmxReader(reader);
            return edmxReader.TryParse(new IEdmModel[] { reference }, out model, out errors);
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <param name="referencedModel">Model to be referenced by the created model.</param>
        /// <returns>The model generated by parsing.</returns>
        public static IEdmModel Parse(XmlReader reader, IEdmModel referencedModel)
        {
            IEdmModel model;
            IEnumerable<EdmError> parseErrors;
            if (!TryParse(reader, referencedModel, out model, out parseErrors))
            {
                throw new EdmParseException(parseErrors);
            }

            return model;
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <param name="references">Models to be referenced by the created model.</param>
        /// <param name="model">The model generated by parsing</param>
        /// <param name="errors">Errors reported while parsing.</param>
        /// <returns>Success of the parse operation.</returns>
        public static bool TryParse(XmlReader reader, IEnumerable<IEdmModel> references, out IEdmModel model, out IEnumerable<EdmError> errors)
        {
            EdmxReader edmxReader = new EdmxReader(reader);
            return edmxReader.TryParse(references, out model, out errors);
        }

        /// <summary>
        /// Returns an IEdmModel for the given EDMX artifact.
        /// </summary>
        /// <param name="reader">XmlReader containing the EDMX artifact.</param>
        /// <param name="referencedModels">Models to be referenced by the created model.</param>
        /// <returns>The model generated by parsing.</returns>
        public static IEdmModel Parse(XmlReader reader, IEnumerable<IEdmModel> referencedModels)
        {
            IEdmModel model;
            IEnumerable<EdmError> parseErrors;
            if (!TryParse(reader, referencedModels, out model, out parseErrors))
            {
                throw new EdmParseException(parseErrors);
            }

            return model;
        }

        /// <summary>
        /// <see cref="Version"/>.TryParse does not exist on all platforms, so implementing it here.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="version">Parsed version.</param>
        /// <returns>False in case of failure.</returns>
        private static bool TryParseVersion(string input, out Version version)
        {
            version = null;

            if (String.IsNullOrEmpty(input))
            {
                return false;
            }

            input = input.Trim();

            var parts = input.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            int major;
            int minor;
            if (!int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor))
            {
                return false;
            }

            version = new Version(major, minor);
            return true;
        }

        private bool TryParse(IEnumerable<IEdmModel> references, out IEdmModel model, out IEnumerable<EdmError> parsingErrors)
        {
            Version edmxVersion;
            try
            {
                this.ParseEdmxFile(out edmxVersion);
            }
            catch (XmlException e)
            {
                model = null;
                parsingErrors = new EdmError[] { new EdmError(new CsdlLocation(e.LineNumber, e.LinePosition), EdmErrorCode.XmlError, e.Message) };
                return false;
            }

            if (this.errors.Count == 0)
            {
                CsdlModel astModel;
                IEnumerable<EdmError> csdlErrors;
                if (this.csdlParser.GetResult(out astModel, out csdlErrors))
                {
                    model = new CsdlSemanticsModel(astModel, new CsdlSemanticsDirectValueAnnotationsManager(), references);
                    
                    Debug.Assert(edmxVersion != null, "edmxVersion != null");
                    model.SetEdmxVersion(edmxVersion);
                    
                    if (this.dataServiceVersion != null)
                    {
                        model.SetDataServiceVersion(this.dataServiceVersion);
                    }

                    if (this.maxDataServiceVersion != null)
                    {
                        model.SetMaxDataServiceVersion(this.maxDataServiceVersion);
                    }
                }
                else
                {
                    Debug.Assert(csdlErrors != null && csdlErrors.Count() > 0, "csdlErrors != null && csdlErrors.Count() > 0");
                    this.errors.AddRange(csdlErrors);
                    model = null;
                }
            }
            else
            {
                model = null;
            }

            parsingErrors = this.errors;
            return this.errors.Count == 0;
        }

        private void ParseEdmxFile(out Version edmxVersion)
        {
            edmxVersion = null;

            // Advance to root element
            if (this.reader.NodeType != XmlNodeType.Element)
            {
                while (this.reader.Read() && this.reader.NodeType != XmlNodeType.Element)
                {
                }
            }

            // There must be a root element for all current artifacts
            if (this.reader.EOF)
            {
                this.RaiseEmptyFile();
                return;
            }

            if (this.reader.LocalName != CsdlConstants.Element_Edmx ||
                !CsdlConstants.SupportedEdmxNamespaces.TryGetValue(this.reader.NamespaceURI, out edmxVersion))
            {
                this.RaiseError(EdmErrorCode.UnexpectedXmlElement, Edm.Strings.XmlParser_UnexpectedRootElement(this.reader.Name, CsdlConstants.Element_Edmx));
                return;
            }

            this.ParseEdmxElement(edmxVersion);
        }

        /// <summary>
        /// All parse functions start with the reader pointing at the start tag of an element, and end after consuming the ending tag for the element.
        /// </summary>
        /// <param name="elementName">The current element name to be parsed.</param>
        /// <param name="elementParsers">The parsers for child elements of the current element.</param>
        private void ParseElement(string elementName, Dictionary<string, Action> elementParsers)
        {
            Debug.Assert(this.reader.LocalName == elementName, "Must call ParseElement on correct element type");
            if (this.reader.IsEmptyElement)
            {
                // Consume the tag.
                this.reader.Read();
            }
            else
            {
                // Consume the start tag.
                this.reader.Read();
                while (this.reader.NodeType != XmlNodeType.EndElement)
                {
                    if (this.reader.NodeType == XmlNodeType.Element)
                    {
                        if (elementParsers.ContainsKey(this.reader.LocalName))
                        {
                            elementParsers[this.reader.LocalName]();
                        }
                        else
                        {
                            this.ParseElement(this.reader.LocalName, EmptyParserLookup);
                        }
                    }
                    else
                    {
                        if (!this.reader.Read())
                        {
                            break;
                        }
                    }
                }

                Debug.Assert(elementName == this.reader.LocalName, "The XmlReader should have thrown an error if the opening and closing tags do not match");

                // Consume the ending tag.
                this.reader.Read();
            }
        }

        private void ParseEdmxElement(Version edmxVersion)
        {
            Debug.Assert(this.reader.LocalName == CsdlConstants.Element_Edmx, "this.reader.LocalName == CsdlConstants.Element_Edmx");
            Debug.Assert(edmxVersion != null, "edmxVersion != null");

            string edmxVersionString = this.GetAttributeValue(null, CsdlConstants.Attribute_Version);
            Version edmxVersionFromAttribute;
            if (edmxVersionString != null && (!TryParseVersion(edmxVersionString, out edmxVersionFromAttribute) || edmxVersionFromAttribute != edmxVersion))
            {
                this.RaiseError(EdmErrorCode.InvalidVersionNumber, Edm.Strings.EdmxParser_EdmxVersionMismatch);
            }

            this.ParseElement(CsdlConstants.Element_Edmx, this.edmxParserLookup);
        }

        private string GetAttributeValue(string namespaceUri, string localName)
        {
            //// OData BufferingXmlReader does not support <see cref="XmlReader.GetAttribute(string)"/> API, so implementing it here.

            string elementNamespace = this.reader.NamespaceURI;
            Debug.Assert(!String.IsNullOrEmpty(elementNamespace), "!String.IsNullOrEmpty(elementNamespace)");

            string value = null;
            bool hasAttributes = this.reader.MoveToFirstAttribute();
            while (hasAttributes)
            {
                if ((namespaceUri != null && this.reader.NamespaceURI == namespaceUri || (String.IsNullOrEmpty(this.reader.NamespaceURI) || this.reader.NamespaceURI == elementNamespace)) &&
                    this.reader.LocalName == localName)
                {
                    value = this.reader.Value;
                    break;
                }

                hasAttributes = this.reader.MoveToNextAttribute();
            }

            // Move back to the element.
            this.reader.MoveToElement();
            return value;
        }

        private void ParseRuntimeElement()
        {
            this.ParseTargetElement(CsdlConstants.Element_Runtime, this.runtimeParserLookup);
        }

        private void ParseDataServicesElement()
        {
            string dataServiceVersionString = this.GetAttributeValue(CsdlConstants.ODataMetadataNamespace, CsdlConstants.Attribute_DataServiceVersion);
            if (dataServiceVersionString != null && !TryParseVersion(dataServiceVersionString, out this.dataServiceVersion))
            {
                this.RaiseError(EdmErrorCode.InvalidVersionNumber, Edm.Strings.EdmxParser_EdmxDataServiceVersionInvalid);
            }

            string maxDataServiceVersionString = this.GetAttributeValue(CsdlConstants.ODataMetadataNamespace, CsdlConstants.Attribute_MaxDataServiceVersion);
            if (maxDataServiceVersionString != null && !TryParseVersion(maxDataServiceVersionString, out this.maxDataServiceVersion))
            {
                this.RaiseError(EdmErrorCode.InvalidVersionNumber, Edm.Strings.EdmxParser_EdmxMaxDataServiceVersionInvalid);
            }

            this.ParseTargetElement(CsdlConstants.Element_DataServices, this.dataServicesParserLookup);
        }

        private void ParseTargetElement(string elementName, Dictionary<string, Action> elementParsers)
        {
            if (!this.targetParsed)
            {
                this.targetParsed = true;
            }
            else
            {
                // Edmx should contain at most one element - either <DataServices> or <Runtime>.
                this.RaiseError(EdmErrorCode.UnexpectedXmlElement, Edm.Strings.EdmxParser_BodyElement(CsdlConstants.Element_DataServices));

                // Read to the end of the element anyway, to let the caller move on to the rest of the document.
                elementParsers = EmptyParserLookup;
            }

            this.ParseElement(elementName, elementParsers);
        }

        private void ParseConceptualModelsElement()
        {
            this.ParseElement(CsdlConstants.Element_ConceptualModels, this.conceptualModelsParserLookup);
        }

        private void ParseCsdlSchemaElement()
        {
            Debug.Assert(this.reader.LocalName == CsdlConstants.Element_Schema, "Must call ParseCsdlSchemaElement on Schema Element");
            using (StringReader sr = new StringReader(this.reader.ReadOuterXml()))
            {
                using (XmlReader xr = XmlReader.Create(sr))
                {
                    this.csdlParser.AddReader(xr);
                }
            }
        }

        private void RaiseEmptyFile()
        {
            this.RaiseError(EdmErrorCode.EmptyFile, Edm.Strings.XmlParser_EmptySchemaTextReader);
        }

        private CsdlLocation Location()
        {
            IXmlLineInfo xmlLineInfo = this.reader as IXmlLineInfo;
            if (xmlLineInfo != null && xmlLineInfo.HasLineInfo())
            {
                return new CsdlLocation(xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
            }

            return new CsdlLocation(0, 0);
        }

        private void RaiseError(EdmErrorCode errorCode, string errorMessage)
        {
            this.errors.Add(new EdmError(this.Location(), errorCode, errorMessage));
        }
    }
}
