using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Maid.Docs.Shared;
public class Documentation
{
	public Documentation()
	{
		DocumentationMembers = new List<DocumentationMember>();
	}
	public List<DocumentationMember> DocumentationMembers { get; set; }
	public void AddDocumentationMember(DocumentationMember documentationMember)
	{
		DocumentationMembers.Add(documentationMember);
	}

}
public record Either<TFirst, TSecond>
{
	public Maybe<TFirst> First { get; init; }
	public Maybe<TSecond> Second { get; init; }
	public Either(TFirst first)
	{
		First = first;
		Second = Maybe<TSecond>.NoValue;
	}
	public Either(TSecond second)
	{
		First = Maybe<TFirst>.NoValue;
		Second = second;
	}


	public TResult Match<TResult>(Func<TFirst, TResult> whenFirst, Func<TSecond, TResult> whenSecond)
	{
		return First.HasValue ? whenFirst(First.Value!) : whenSecond(Second.Value!);
	}

	public static implicit operator Either<TFirst, TSecond>(TFirst first) => new(first);
	public static implicit operator Either<TFirst, TSecond>(TSecond second) => new(second);
}
public record Maybe<TType>
{
	public TType? Value { get; init; }
	public bool HasValue { get; init; }
	public Maybe(TType value) => (Value, HasValue) = (value, value is not null);

	public static Maybe<TType> NoValue => new(default!);

	public static implicit operator Maybe<TType>(TType value) => new(value);

	public bool Eval(out TType value)
	{
		value = default!;
		if (HasValue)
		{
			value = Value!;
		}
		return HasValue;
	}
}

public abstract record LinkBase(string FriendlyName)
{
}
public record ExternalLink(string FriendlyName) : LinkBase(FriendlyName)
{

}
public record InternalLink(string FriendlyName, IdentifierBase Identifier) : LinkBase(FriendlyName)
{

}
public abstract record IdentifierBase(string Namespace, string AssemblyName)
{ }

public record ClassIdentifier(string Namespace, string ClassName, string AssemblyName)
	: IdentifierBase(Namespace, AssemblyName)
{ }
public record InterfaceIdentifier(string Namespace, string InterfaceName, string AssemblyName)
	: IdentifierBase(Namespace, AssemblyName)
{ }
public record ClassMemberIdentifier(ClassIdentifier ClassIdentifier, string MemberName)
	: IdentifierBase(ClassIdentifier.Namespace, ClassIdentifier.AssemblyName)
{ }
public record MethodIdentifier(ClassIdentifier ClassIdentifier, string MethodName)
	: ClassMemberIdentifier(ClassIdentifier, MethodName)
{ }
public record FieldIdentifier(ClassIdentifier ClassIdentifier, string FieldName)
	: ClassMemberIdentifier(ClassIdentifier, FieldName)
{ }
public record PropertyIdentifier(ClassIdentifier ClassIdentifier, string PropertyName)
	: ClassMemberIdentifier(ClassIdentifier, PropertyName)
{ }
public record ConstructorIdentifier(ClassIdentifier ClassIdentifier)
	: ClassMemberIdentifier(ClassIdentifier, ClassIdentifier.ClassName)
{ }
public abstract record DocumentationMemberIdBase(LinkBase LinkBase);
public abstract record DocumentationMemberIdBase<TLink>(string FriendlyName, TLink Url) : DocumentationMemberIdBase(Url) where TLink : LinkBase
{ }
public record InternalDocumentationMemberId(string FriendlyName, InternalLink Url) : DocumentationMemberIdBase<InternalLink>(FriendlyName, Url)
{ }
public record ExternalDocumentationMemberId(string FriendlyName, ExternalLink Url) : DocumentationMemberIdBase<ExternalLink>(FriendlyName, Url)
{ }
public abstract class DocumentationMember
{
	public DocumentationMember(IdentifierBase identifier, DocumentationMemberIdBase documentationMemberId)
	{
		Identifier = identifier;
		DocumentationMemberIdBase = documentationMemberId;
	}
	public IdentifierBase Identifier { get; set; }
	public DocumentationMemberIdBase DocumentationMemberIdBase
	{
		get; set;
	}
	public string DocumentationXML { get; set; } = string.Empty;
}
public abstract class TypeMemberDocumentation(IdentifierBase TypeIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
		: DocumentationMember(TypeIdentifier, InternalDocumentationMemberId)
{
	public required string Accessibility { get; set; }
	public required bool IsStatic { get; set; }
	public required bool IsAbstract { get; set; }
	public required bool IsSealed { get; set; }
	public required bool IsVirtual { get; set; }
	public required bool IsPartial { get; set; }
}
public class TypeDocumentation(IdentifierBase TypeIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: DocumentationMember(TypeIdentifier, InternalDocumentationMemberId)
{
	public List<TypeMemberDocumentation> TypeMembers { get; set; } = new();


	public void AddTypeMember(TypeMemberDocumentation classMemberDocumentation)
	{
		TypeMembers.Add(classMemberDocumentation);
	}

	public TypeDocumentation WithMembers(List<TypeMemberDocumentation> classMembers)
	{
		TypeMembers = classMembers;
		return this;
	}
}
public class InterfaceDocumentation(InterfaceIdentifier InterfaceIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
: TypeDocumentation(InterfaceIdentifier, InternalDocumentationMemberId)
{
}

public class MethodDocumentation(MethodIdentifier MethodIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: TypeMemberDocumentation(MethodIdentifier, InternalDocumentationMemberId)
{
	public List<(IdentifierBase type, string argumentName)> Arguments { get; set; } = new();
}
public class FieldDocumentation(FieldIdentifier FieldIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: TypeMemberDocumentation(FieldIdentifier, InternalDocumentationMemberId)
{
	public required IdentifierBase FieldType { get; set; }
}
public class PropertyDocumentation(PropertyIdentifier PropertyIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: TypeMemberDocumentation(PropertyIdentifier, InternalDocumentationMemberId)
{
	public required IdentifierBase PropertyType { get; set; }
}
public class ConstructorDocumentation(ConstructorIdentifier ConstructorIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: TypeMemberDocumentation(ConstructorIdentifier, InternalDocumentationMemberId)
{
	public List<(IdentifierBase type, string argumentName)> Arguments { get; set; } = new();
}
public class ClassDocumentation(ClassIdentifier ClassIdentifier, InternalDocumentationMemberId InternalDocumentationMemberId)
	: TypeDocumentation(ClassIdentifier, InternalDocumentationMemberId)
{ }