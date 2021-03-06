﻿namespace Microsoft.RestApi.Transformers
{
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Microsoft.RestApi.Models;
    using System.Collections.Generic;
    using System.Linq;

    public static class TransformHelper
    {
        public static IList<string> Errors = new List<string>();
        public static HashSet<string> PrimitiveTypes = new HashSet<string> { "integer", "number", "string", "boolean" };

        public static PropertyTypeEntity ParseOpenApiSchema(string schemaName, OpenApiSchema openApiSchema, TransformModel transformModel, ref Dictionary<string, OpenApiSchema> needExtractedSchemas, bool isComponent = false)
        {
            var type = new PropertyTypeEntity();
            type.ReferencedType = openApiSchema.Type;

            if (isComponent)
            {
                type.Summary = openApiSchema.Description;
            }

            if (openApiSchema.Type == "array")
            {
                if (openApiSchema.Items?.Enum?.Count > 0)
                {
                    type.ReferencedType = openApiSchema.Items.Type;
                    type.Values = GetValueFromListAny(openApiSchema.Items.Enum).ToList();
                }
                else
                {
                    type.ReferencedType = openApiSchema.Items?.Reference != null ?
                        Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), openApiSchema.Items.Reference.Id) :
                        openApiSchema.Items?.Type;
                    if (type.ReferencedType == "array" || type.ReferencedType == "object")
                    {
                        if (needExtractedSchemas == null) needExtractedSchemas = new Dictionary<string, OpenApiSchema>();
                        var extractedName = GetExtractedName(schemaName);
                        if (needExtractedSchemas.ContainsKey(extractedName))
                        {
                            Errors.Add($"Please move schema definition in property \"{schemaName}\" to schema components in file {transformModel.SourceFilePath}");
                        }
                        else
                        {
                            needExtractedSchemas.Add(extractedName, openApiSchema.Items);
                            type.ReferencedType = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), extractedName);
                        }
                    }
                }
                type.IsArray = true;
            }
            else if (openApiSchema.Type == "object" || openApiSchema.Properties?.Count > 0 || openApiSchema.Type == null || openApiSchema.Reference != null)
            {
                if (openApiSchema.Reference != null && !isComponent)
                {
                    type.ReferencedType = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), openApiSchema.Reference.Id);
                }
                else if (openApiSchema.AdditionalProperties != null)
                {
                    type.IsDictionary = true;
                    if (PrimitiveTypes.Contains(openApiSchema.AdditionalProperties.Type))
                    {
                        type.ReferencedType = openApiSchema.AdditionalProperties.Type;
                    }
                    else if (openApiSchema.AdditionalProperties.Reference == null)
                    {
                        OpenApiReference reference = null;
                        SetExtractedSchemas(schemaName, openApiSchema.AdditionalProperties, transformModel, needExtractedSchemas, ref reference);

                        if (reference != null)
                        {
                            type.ReferencedType = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), reference.Id);
                        }
                    }
                    else
                    {
                        type.ReferencedType = Utility.GetId(transformModel.ServiceId, transformModel.SourceFileName, ComponentGroup.Schemas.ToString(), openApiSchema.AdditionalProperties.Reference.Id);
                    }
                }
                else if (openApiSchema.Properties?.Count > 0)
                {
                    type.Properties = new List<PropertyEntity>();
                    type.Properties.AddRange(GetPropertiesFromSchema(openApiSchema, transformModel, ref needExtractedSchemas));
                    type.ReferencedType = null;
                }
            }
            else
            {
                if (openApiSchema.Enum?.Count > 0)
                {
                    type.Values = GetValueFromListAny(openApiSchema.Enum).ToList();
                }
            }

            if (isComponent)
            {
                type.Service = transformModel.ServiceName;
                type.ApiVersion = transformModel.OpenApiDoc.Info.Version;
            }

            if (type.ReferencedType != null && PrimitiveTypes.Contains(type.ReferencedType))
            {
                type.SimpleType = type.ReferencedType;
                type.ReferencedType = null;
            }

            return type;
        }

        public static IList<PropertyEntity> GetPropertiesFromSchema(OpenApiSchema openApiSchema, TransformModel transformModel, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var properties = new List<PropertyEntity>();
            var required = openApiSchema.Required;
            if (openApiSchema.Type == "object" || openApiSchema.Properties?.Count > 0)
            {
                foreach (var property in openApiSchema.Properties)
                {
                    properties.Add(new PropertyEntity
                    {
                        Name = property.Key,
                        IsRequired = required.Contains(property.Key),
                        IsDeprecated = property.Value.Deprecated,
                        Nullable = property.Value.Nullable,
                        Description = property.Value.Description ?? property.Value.Title,
                        Pattern = property.Value.Pattern,
                        Format = property.Value.Format,
                        IsAnyOf = property.Value.AnyOf?.Count() > 0,
                        IsOneOf = property.Value.OneOf?.Count() > 0,
                        IsAllOf = property.Value.AllOf?.Count() > 0,
                        IsNot = property.Value.Not != null,
                        Types = ExtractProperty(property, transformModel, ref needExtractedSchemas)
                    });
                }
            }

            return properties;
        }

        public static string GetValueFromPrimitiveType(IOpenApiAny anyPrimitive)
        {
            if (anyPrimitive is OpenApiInteger integerValue)
            {
                return integerValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiLong longValue)
            {
                return longValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiFloat floatValue)
            {
                return floatValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDouble doubleValue)
            {
                return doubleValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiString stringValue)
            {
                return stringValue.Value;
            }
            if (anyPrimitive is OpenApiByte byteValue)
            {
                return byteValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiBinary binaryValue)
            {
                return binaryValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiBoolean boolValue)
            {
                return boolValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDate dateValue)
            {
                return dateValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiDateTime dateTimeValue)
            {
                return dateTimeValue.Value.ToString();
            }
            if (anyPrimitive is OpenApiPassword passwordValue)
            {
                return passwordValue.Value.ToString();
            }
            return string.Empty;
        }

        public static IList<string> GetValueFromListAny(IList<IOpenApiAny> anyList)
        {
            var results = new List<string>();
            foreach (var anyValue in anyList)
            {
                if (anyValue.AnyType == AnyType.Primitive)
                {
                    results.Add(GetValueFromPrimitiveType(anyValue));
                }
            }
            return results.OrderBy(r => r).ToList();
        }

        public static string GetExtractedName(string propertyName)
        {
            return string.IsNullOrEmpty(propertyName) ? string.Empty : propertyName + "Param";
        }

        public static string GetRawPropertyName(string extractedName)
        {
            if (string.IsNullOrEmpty(extractedName)) return extractedName;
            return extractedName.EndsWith("Param") ? extractedName.Substring(0, extractedName.Length - 5) : extractedName;
        }

        public static List<PropertyTypeEntity> ExtractProperty(KeyValuePair<string, OpenApiSchema> property, TransformModel transformModel, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            var types = new List<PropertyTypeEntity>();
            if (property.Value.AnyOf?.Count() > 0)
            {
                types = ExtractTypes(property.Key, property.Value.AnyOf.ToList(), transformModel, ref needExtractedSchemas);
            }
            else if (property.Value.OneOf?.Count() > 0)
            {
                types = ExtractTypes(property.Key, property.Value.OneOf.ToList(), transformModel, ref needExtractedSchemas);
            }
            else if (property.Value.AllOf?.Count() > 0)
            {
                types = ExtractTypes(property.Key, property.Value.AllOf.ToList(), transformModel, ref needExtractedSchemas);
            }
            else if (property.Value.Not != null)
            {
                types = ExtractTypes(property.Key, new List<OpenApiSchema> { property.Value.Not }, transformModel, ref needExtractedSchemas);
            }
            else
            {
                types = ExtractTypes(property.Key, new List<OpenApiSchema> { property.Value }, transformModel, ref needExtractedSchemas);
            }
            return types;
        }

        public static List<PropertyTypeEntity> ExtractTypes(string propertyName, List<OpenApiSchema> schemas, TransformModel transformModel, ref Dictionary<string, OpenApiSchema> needExtractedSchemas)
        {
            if (schemas?.Count < 1) return null;

            if (schemas?.Count > 1 && schemas.Any(schema => schema.Reference == null && schema.Properties?.Count > 0))
            {
                Errors.Add($"Please move schema definition in property \"{propertyName}\" to schema components in file {transformModel.SourceFilePath}");
                return null;
            }

            var types = new List<PropertyTypeEntity>();
            foreach (var schema in schemas)
            {
                if (schema.Reference == null && schema.Properties?.Count > 0)
                {
                    OpenApiReference reference = null;

                    SetExtractedSchemas(propertyName, schema, transformModel, needExtractedSchemas, ref reference);

                    if (reference == null) return null;

                    schema.Reference = new OpenApiReference { Id = GetExtractedName(propertyName) };
                }

                types.Add(ParseOpenApiSchema(propertyName, schema, transformModel, ref needExtractedSchemas));
            }

            return types;
        }

        public static void SetExtractedSchemas(string propertyName, OpenApiSchema schema, TransformModel transformModel, Dictionary<string, OpenApiSchema> needExtractedSchemas, ref OpenApiReference reference)
        {
            if (needExtractedSchemas == null) needExtractedSchemas = new Dictionary<string, OpenApiSchema>();

            var extractedName = GetExtractedName(propertyName);
            if (needExtractedSchemas.ContainsKey(extractedName))
            {
                Errors.Add($"Please move schema definition in property \"{propertyName}\" to schema components in file {transformModel.SourceFilePath}");
            }
            else
            {
                needExtractedSchemas.Add(extractedName, schema);
                reference = new OpenApiReference { Id = extractedName };
            }
        }
    }
}
