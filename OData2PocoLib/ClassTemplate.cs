// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.
#pragma warning disable S1210
using OData2Poco.CustAttributes;

namespace OData2Poco;

/// <summary>
///     Define the propertis of the class
/// </summary>
public sealed class ClassTemplate : IEquatable<ClassTemplate?>, IComparable<ClassTemplate>
{
    private readonly AttributeFactory _attributeFactory = AttributeFactory.Default;

    private ClassTemplate()
    {
        Properties = new List<PropertyTemplate>();
        Keys = new List<string>();
        EnumElements = new List<string>();
        Navigation = new List<string>();
        BaseType = "";
        Comment = "";
        Name = "UNDEFINED";
        NameSpace = "";
    }

    public ClassTemplate(int id) : this()
    {
        Id = id;
    }

    public int Id { get; }
    public string Name { get; set; }
    public string BaseType { get; set; }
    public string? Comment { get; set; }
    public List<PropertyTemplate> Properties { get; set; }
    public List<string> Keys { get; set; }
    public List<string> Navigation { get; set; }

    //to support enum generation code
    public bool IsEnum { get; set; }
    public bool IsFlags { get; set; } //v3, Add [FlagsAttribute] to enum
    public List<string> EnumElements { get; set; }
    public string OriginalName { get; set; } = null!;

    //v1.4.0
    public string? EntitySetName { get; set; }
    public string NameSpace { get; set; }

    public string FullName => string.IsNullOrEmpty(NameSpace)
        ? Name
        : $"{NameSpace}.{Name}";

    public bool IsComplex { get; set; }
    public bool IsEntity { get; set; }
    public bool IsAbstrct { get; set; }

    public List<string> GetAllAttributes()
    {
        return _attributeFactory.GetAllAttributes(this);
    }

    public override string ToString()
    {
        // 'id:name(parent)'
        return string.IsNullOrEmpty(BaseType)
            ? $"{Id}:{Name}"
            : $"{Id}:{Name}({BaseType.Split('.').Last()})";
    }

    #region IEquatable and Comparer

    public override bool Equals(object? obj)
    {
        return Equals(obj as ClassTemplate);
    }

    public bool Equals(ClassTemplate? other)
    {
        return other is not null && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public int CompareTo(ClassTemplate? other)
    {
        if (BaseType == other?.FullName)
            return 1;
        if (FullName == other?.BaseType)
            return -1;
        return string.Compare(FullName, other?.FullName, StringComparison.Ordinal);
    }

    #endregion
}