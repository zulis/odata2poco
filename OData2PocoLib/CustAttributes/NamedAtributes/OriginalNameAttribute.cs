// Copyright (c) Mohamed Hassan & Contributors. All rights reserved. See License.md in the project root for license information.



// ReSharper disable UnusedMember.Global

namespace OData2Poco.CustAttributes.NamedAtributes;

/*
  Method to keep originalName after renaming name due to conflication with reserved keywords
   [OriginalName("Applicant")]
    //or
    [DataMember(Name = "Applicant")]
    //or
     [JsonProperty("Applicant")]
    public virtual string applicant {get;set;}  //renamed to applicant
 */
public class OriginalNameAttribute : INamedAttribute
{
    public string Name { get; } = "original";

    public List<string> GetAttributes(PropertyTemplate property)
    {
        return new List<string>
        {
            property.OriginalName != property.PropName
                ? $"[JsonProperty(\"{property.OriginalName}\")]"
                : string.Empty
        };
    }

    public List<string> GetAttributes(ClassTemplate classTemplate)
    {
        return new();
    }
}