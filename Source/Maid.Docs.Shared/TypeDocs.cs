namespace Maid.Docs.Shared;

public enum AccessModifier
{
	Public,
	Protected,
	Internal,
	Private,
	ProtectedOrInternal,
	ProtectedAndInternal
}


public record Either<TFirst, TSecond>
{
	public TFirst First { get; set; }
	public TSecond Second { get; set; }
	
	public static Either<TFirst, TSecond> CreateFirst(TFirst first) => new() { First = first };
	public static Either<TFirst, TSecond> CreateSecond(TSecond second) => new() { Second = second };

	public bool IsFirst => First is not null;
	public bool IsSecond => Second is not null;

	public static implicit operator Either<TFirst, TSecond>(TFirst first) => CreateFirst(first);
	public static implicit operator Either<TFirst, TSecond>(TSecond second) => CreateSecond(second);

	public T Match<T>(Func<TFirst, T> first, Func<TSecond, T> second) => IsFirst ? first(First) : second(Second);
}

	public record DocsRef();

public class Docs
{
	public string DocId { get; set; }
	public List<DocsMember> DocsMembers { get; set; } = [];
}

public record DocsMemberRef(string DocId, string DocMemberId) : DocsRef
{
	//create const for empty
	public static DocsMemberRef Empty { get; } = new(string.Empty, string.Empty);

	public static DocsMemberRef Create(string DocId, string DocMemberId) => new(DocId, DocMemberId);
}
public record DocsExternalMemberRef(string Name) : DocsRef
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
	public string ExternalLink { get; set; } = string.Empty;
	public static ExternalAttribute Create(string Name, (string, string)[] Arguments) => new (Name, Arguments);
};

public class DocsMember
{
	public string DocId { get; set; } = string.Empty;
	public string DocMemberId { get; set; } = string.Empty;
}

public class TypeDocs : DocsMember
{
	public TypeDocs(string docId, string docMemberId)
	{
		DocId = docId;
		DocMemberId = docMemberId;
	}
	public string TypeName { get; set; }
	public Either<DocsRef, string> Namespace { get; set; }
	public string AssemblyName { get; set; }
	public AccessModifier AccessModifier { get; set; }
	public List<MethodDocs> Methods { get; set; } = [];
	public List<PropertyDoc> Properties { get; set; } = [];
	public List<FieldDoc> Fields { get; set; } = [];
	public List<Attribute> Attributes { get; set; } = [];

	public Either<DocsRef, string> InheritedType { get; set; }

	public TypeDocs Configure(Action<TypeDocs> configure)
	{
		configure(this);
		return this;
	}
}

public class MethodDocs : DocsMember
{
	public MethodDocs(string docId, string docMemberId)
	{
		DocId = docId;
		DocMemberId = docMemberId;
	}
	public string MethodName { get; set; }
	public DocsRef ReturnType { get; set; }
	public DocsMemberRef IsOverride { get; set; }
	public AccessModifier AccessModifier { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	public List<string> Modifiers { get; set; } = [];

	public MethodDocs Configure(Action<MethodDocs> configure)
	{
		configure(this);
		return this;
	}
}

[Flags]
public enum PropertyAccessor
{
	Get = 0,
	Set = 1 << 0,
	Init = 1 << 1,
}

public class PropertyDoc : DocsMember
{
	public PropertyDoc(string docId, string docMemberId)
	{
		DocId = docId;
		DocMemberId = docMemberId;
	}
	public string PropertyName { get; set; } = string.Empty;
	public string PropertyType { get; set; } = string.Empty;
	public PropertyAccessor Method { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	// TODO: add whether property is {get; set; } or {get;} or {set;} or {get; init;}


	public List<string> Modifiers { get; set; } = [];

	public PropertyDoc Configure(Action<PropertyDoc> configure)
	{
		configure(this);
		return this;
	}
}

public class FieldDoc : DocsMember
{
	public FieldDoc(string docId, string docMemberId)
	{
		DocId = docId;
		DocMemberId = docMemberId;
	}
	public string FieldName { get; set; } = string.Empty;
	public string FieldType { get; set; } = string.Empty;
	public AccessModifier AccessModifier { get; set; }
	public List<Attribute> Attributes { get; set; } = [];

	public List<string> Modifiers { get; set; } = [];

	public FieldDoc Configure(Action<FieldDoc> configure)
	{
		configure(this);
		return this;
	}
}



