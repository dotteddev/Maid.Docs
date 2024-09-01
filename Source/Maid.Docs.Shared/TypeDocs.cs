namespace Maid.Docs.Shared;

public enum AccessModifier
{
	Public,
	Protected,
	Internal,
	Private,
}

public record DocsRef();

public record DocsMemberRef(string DocId, string DocMemberId) : DocsRef
{
	//create const for empty
	public static DocsMemberRef Empty { get; } = new(string.Empty, string.Empty);

	public static DocsMemberRef Create(string DocId, string DocMemberId) => new(DocId, DocMemberId);
}
public record DocsExternalMemberRef() : DocsRef
{
	public string ExternalLink { get; set; } = string.Empty;
}
public record Attribute(string Name, (string, string)[] Arguments);

public record OurAttribute(string Name, (string, string)[] Arguments, DocsMemberRef DocsMemberRef) : Attribute(Name, Arguments)
{
	public static OurAttribute Create(string Name, (string, string)[] Arguments, DocsMemberRef DocsMemberRef) => new(Name, Arguments, DocsMemberRef);
};
public record ExternalAttribute(string Name, (string, string)[] Arguments): Attribute(Name,Arguments)
{
	public DocsExternalMemberRef ExternalLink { get; set; } = new();
	public static ExternalAttribute Create(string Name, (string, string)[] Arguments) => new (Name, Arguments);
};

public class DocsMember(string DocId, string DocMemberId)
{
	public static DocsMember Create(string DocId, string DocMemberId) => new(DocId, DocMemberId);
}

public class TypeDocs(string DocId, string DocMemberId) : DocsMember(DocId, DocMemberId)
{
	public string TypeName { get; set; }
	public DocsRef Namespace { get; set; }
	public string AssemblyName { get; set; }
	public AccessModifier AccessModifier { get; set; }
	public List<MethodDocs> Methods { get; set; } = [];
	public List<PropertyDoc> Properties { get; set; } = [];
	public List<FieldDoc> Fields { get; set; } = [];
	public List<Attribute> Attributes { get; set; } = [];

	public DocsRef InheritedType { get; set; }

}

public class MethodDocs(string DocId, string DocMemberId) : DocsMember(DocId, DocMemberId)
{
	public string MethodName { get; set; }
	public string ReturnType { get; set; }
	public DocsMemberRef IsOverride { get; set; }
	public AccessModifier AccessModifier { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	public List<string> Modifiers { get; set; } = [];

}

[Flags]
public enum PropertyAccessor
{
	Get = 0,
	Set = 1 << 0,
	Init = 1 << 1,
}

public class PropertyDoc(string DocId, string DocMemberId) : DocsMember(DocId, DocMemberId)
{
	public string PropertyName { get; set; } = string.Empty;
	public string PropertyType { get; set; } = string.Empty;
	public PropertyAccessor Method { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	// TODO: add whether property is {get; set; } or {get;} or {set;} or {get; init;}


	public List<string> Modifiers { get; set; } = [];
}

public class FieldDoc(string DocId, string DocMemberId) : DocsMember(DocId, DocMemberId)
{
	public string FieldName { get; set; } = string.Empty;
	public string FieldType { get; set; } = string.Empty;
	public AccessModifier AccessModifier { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	public List<string> Modifiers { get; set; } = [];
}



