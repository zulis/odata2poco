using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using OData2Poco.Extensions;
using OData2Poco.InfraStructure.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using Microsoft.OData.Edm.Csdl;

namespace OData2Poco.V4
{
    /// <summary>
    ///     Process metadataString and generate list of   classes
    /// </summary>
    internal partial class Poco : IPocoGenerator
    {       
        public MetaDataInfo MetaData { get; set; }
        public string MetaDataAsString => MetaData.MetaDataAsString;
        private readonly PocoSetting _setting;
        private IEnumerable<IEdmEntitySet> EntitySets { get; set; }
        private readonly ILog _logger = PocoLogger.Default;
        private List<string> SchemaErrors { get; set; }
        private IEdmModel Model { get; set; }
        internal Poco(MetaDataInfo metaData, PocoSetting setting)
        {
            _setting = setting ?? new PocoSetting();
            MetaData = metaData;
            SchemaErrors = new List<string>();
            EntitySets = new List<IEdmEntitySet>();
            Model= LoadModelFromString();
        }
        //support Odata.Edm v7+
        internal  IEdmModel LoadModelFromString()
        {
            IEdmModel model;
            //Microsoft.OData.Edm" v7+
            //breaking change in Odata.Edm in v7+
            var tr = new StringReader(MetaDataAsString);
            var reader = XmlReader.Create(tr);
            try
            {
                CsdlReader.TryParse(reader, true, out model, out _);
                EntitySets = GetEntitySets(model);
            }
            finally
            {
                ((IDisposable)reader).Dispose();
            }
            return model;
        }
        private List<string> GetEnumElements(IEdmSchemaType type, out bool isFlags)
        {
            var enumList = new List<string>();
            isFlags = false;
            if (type.TypeKind != EdmTypeKind.Enum) return enumList;
            if (!(type is IEdmEnumType enumType)) return enumList;
            var list2 = enumType.Members;
            isFlags = enumType.IsFlags;
            foreach (var item in list2)
            {
                //issue12 reserved keyword
                var enumElement = $"\t\t{item.Name.ChangeReservedWord()}={item.Value.Value}";
                //enumList.Add(item.Name); // v2.3.0
                enumList.Add(enumElement); //issue #7 complete enum name /value
            }

            return enumList;
        }

        private string GetEntitySetName(IEdmSchemaType ct)
        {
            if (ct.TypeKind != EdmTypeKind.Entity)
                return string.Empty;
            var entitySet = EntitySets
                .Where(m => m != null && m.EntityType().FullName() == ct.FullName())
             .DefaultIfEmpty().First();
            return entitySet != null ? entitySet.Name : string.Empty;
        }

        internal IEnumerable<IEdmEntitySet> GetEntitySets(IEdmModel model)
        {
            var entitySets = model.EntityContainer.EntitySets();
            return entitySets;
        }

        /// <summary>
        ///     Fill List from Model with class name and properties of corresponding entitie to be used for generating code
        /// </summary>
        /// <returns></returns>
        public List<ClassTemplate> GeneratePocoList()
        {
            var list = new List<ClassTemplate>();            
            IEnumerable<IEdmSchemaType> schemaElements = GetSchemaElements(Model);
            foreach (var type in schemaElements.ToList())
            {
                var ct = GeneratePocoClass(type);
                if (ct == null) continue;
                list.Add(ct);
            }
            return list;
        }

        internal ClassTemplate? GeneratePocoClass(IEdmSchemaType ent)
        {
            if (ent == null) return null;
            var className = ent.Name;
            var classTemplate = new ClassTemplate
            {
                Name = className,
                OriginalName = className,
                IsEnum = ent is IEdmEnumType,
                NameSpace = ent.Namespace,

            };

            switch (ent)
            {
                case IEdmEnumType enumType:
                    {
                        classTemplate.EnumElements = GetEnumElements(enumType, out var isFlags); //fill enum elements for enumtype
                        classTemplate.IsFlags = isFlags;
                        return classTemplate;
                    }

                case IEdmEntityType entityType:
                    {
                        classTemplate.IsEntity = true;
                        classTemplate.IsAbstrct = entityType.IsAbstract;
                        // Set base type by default
                        var baseEntityType = entityType.BaseEntityType();
                        if (baseEntityType != null) classTemplate.BaseType = baseEntityType.FullName(); //.Name
                        classTemplate.EntitySetName = GetEntitySetName(ent);
                        break;
                    }
                //parent of complex types
                case IEdmComplexType complexType:
                    {
                        classTemplate.IsComplex = true;
                        classTemplate.IsAbstrct = complexType.IsAbstract;
                        if (complexType.BaseType != null)
                            classTemplate.BaseType = complexType.BaseType.ToString() ?? "";
                        break;
                    }
                default:
                    return null;
            }

            //fill keys 
            var list = GetKeys(ent);
            if (list != null) classTemplate.Keys.AddRange(list);

            //fill navigation properties
            var list2 = GetNavigation(ent);
            if (list2 != null) classTemplate.Navigation.AddRange(list2);

            var entityProperties = GetClassProperties(ent);

            //set the key ,comment
            foreach (var property in entityProperties)
            {
                property.ClassName = className;
                property.OriginalName = property.PropName;
                if (classTemplate.Navigation.Exists(x => x == property.PropName)) property.IsNavigate = true;

                if (classTemplate.Keys.Exists(x => x == property.PropName)) property.IsKey = true;
                var comment = (property.IsKey ? "PrimaryKey" : string.Empty)
                              + (property.IsNullable ? string.Empty : " not null");
                if (!string.IsNullOrEmpty(comment)) property.PropComment = comment;
            }

            classTemplate.Properties.AddRange(entityProperties);
            return classTemplate;
        }

        private List<string> GetNavigation(IEdmSchemaType ent)
        {
            var list = new List<string>();
            if (!(ent is IEdmEntityType entityType)) return list;
            var nav = entityType.DeclaredNavigationProperties().ToList();
            list.AddRange(nav.Select(key => key.Name));
            return list;
        }

        private List<string> GetKeys(IEdmSchemaType ent)
        {
            var list = new List<string>();
            if (!(ent is IEdmEntityType entityType)) return list;
            var keys = entityType.DeclaredKey;
            if (keys != null)
                list.AddRange(keys.Select(key => key.Name));
            return list;
        }

        private List<PropertyTemplate> GetClassProperties(IEdmSchemaType ent)
        {
            //stop here if enum
            if (ent is IEdmEnumType) return Enumerable.Empty<PropertyTemplate>().ToList(); 
            var structuredType = ent as IEdmStructuredType;
            var properties = structuredType.Properties();
            if (_setting != null && _setting.UseInheritance)
            {
                properties = properties.Where(x => x.DeclaringType.FullTypeName() == ent.FullTypeName());
            }

            //add serial for properties to support protbuf v3.0
            var serial = 1;
            var list = properties.Select(property => new PropertyTemplate
            {
                //all reference types are null by default (c#8). 
                IsNullable = property.Type.IsPrimitive() || property.Type.IsEnum()
                    ? property.Type.IsNullable : true,
                PropName = property.Name,
                PropType = GetClrTypeName(property.Type),
                Serial = serial++,
                ClassNameSpace = ent.Namespace,
                MaxLength = GetMaxLength(property),
                IsReadOnly = Model.IsReadOnly(property),
                //OriginalType = property.VocabularyAnnotations(Model),
            }).ToList();

            return list;
        }
        int? GetMaxLength(IEdmProperty property)
        {
            int? maxLength = null;
            switch (property.Type.PrimitiveKind())
            {
                case EdmPrimitiveTypeKind.String:
                    maxLength = property.Type.AsString().MaxLength;
                    break;
                case EdmPrimitiveTypeKind.Binary:
                    maxLength = property.Type.AsBinary().MaxLength;
                    break;
            }
            return maxLength ?? 0;
        }
        //fill all properties/name of the class template
        private string GetClrTypeName(IEdmTypeReference edmTypeReference)
        {
            CheckError(edmTypeReference);
            string? clrTypeName = edmTypeReference.ToString() ?? "UNDEFINED";
            var edmType = edmTypeReference.Definition;
            if (edmTypeReference.IsPrimitive())
                if (edmType != null)
                    return EdmToClr((IEdmPrimitiveType)edmType);
            if (edmTypeReference.IsEnum())
            {
                if (edmType is IEdmEnumType ent)
                    return ent.FullName();
            }

            if (edmTypeReference.IsComplex())
            {

                if (edmType is IEdmComplexType edmComplexType)
                    return edmComplexType.FullName();
            }

            if (edmTypeReference.IsEntity())
            {
                if (edmType is IEdmEntityType ent)
                    return ent.FullName();
            }

            if (edmTypeReference.IsEntityReference())
            {
                if (edmType is IEdmEntityType ent)
                    return ent.FullName();
            }


            if (!edmTypeReference.IsCollection()) return clrTypeName;
            if (!(edmType is IEdmCollectionType edmCollectionType)) return clrTypeName;
            var elementTypeReference = edmCollectionType.ElementType;
            if (!(elementTypeReference.Definition is IEdmPrimitiveType primitiveElementType))
            {
                if (!(elementTypeReference.Definition is IEdmSchemaElement schemaElement)) return clrTypeName;
                clrTypeName = schemaElement.FullName();
                clrTypeName = $"List<{clrTypeName}>";
                return clrTypeName;
            }

            clrTypeName = EdmToClr(primitiveElementType);
            clrTypeName = $"List<{clrTypeName}>";
            return clrTypeName;
        }

        private string EdmToClr(IEdmPrimitiveType type)
        {
            var kind = type.PrimitiveKind;
            return ClrDictionary.ContainsKey(kind)
                ? ClrDictionary[kind]
                : kind.ToString();
        }

        private bool CheckError(IEdmTypeReference edmTypeReference)
        {
            var edmType = edmTypeReference.Definition;
            if (!edmType.Errors().Any()) return false;
            var error = edmType.Errors().Select(x => $"Location: {x.ErrorLocation}, {x.ErrorMessage}").FirstOrDefault();
            _logger.Trace($"edmTypeReference Error: {error.Dump()}");
            _logger.Warn($"Invalid Type Reference: {edmType}");
            SchemaErrors.Add($"Invalid Type Reference: {edmType}");
            return true;

        }

        #region Helper Methods

        internal IEdmSchemaType? GetSchemaType(string name, string nameSpace)
        {
            return (IEdmSchemaType?)Model.SchemaElements.FirstOrDefault(x => x.Name == name && x.Namespace == nameSpace);
        }
        internal IEdmSchemaType? GetSchemaType(string fullName)
        {
            return (IEdmSchemaType?)Model.SchemaElements.FirstOrDefault(x => x.FullName() == fullName);
        }
        IEnumerable<IEdmSchemaType> GetSchemaElements(IEdmModel model)
        {
            return model.SchemaElements.OfType<IEdmSchemaType>();
        }

        internal IEnumerable<IEdmSchemaType> GetSchemaElements(Func<IEnumerable<IEdmSchemaType>, IEnumerable<IEdmSchemaType>> func)
        {
            var elements = Model.SchemaElements.OfType<IEdmSchemaType>();
            return func(elements);

        }
        #endregion

    }
}