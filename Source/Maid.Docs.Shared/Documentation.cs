using System;
using System.Diagnostics;
using System.Linq;


namespace Maid.Docs.Shared;
public class Documentation
{
	public Documentation()
	{
		DocumentationMembers = [];
	}

	public void AddDocumentationMember(MemberDocumentationBase documentationMember)
	{
		DocumentationMembers.Add(documentationMember);
	}

	public List<MemberDocumentationBase> DocumentationMembers { get; set; }

}
public record Either<TFirst, TSecond>
{
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

	public static implicit operator Either<TFirst, TSecond>(TFirst first) => new(first);
	public static implicit operator Either<TFirst, TSecond>(TSecond second) => new(second);


	public TResult Match<TResult>(Func<TFirst, TResult> whenFirst, Func<TSecond, TResult> whenSecond)
	{
		return First.HasValue ? whenFirst(First.Value!) : whenSecond(Second.Value!);
	}

	public Maybe<TFirst> First { get; init; }
	public Maybe<TSecond> Second { get; init; }
}
public record Maybe<TType>
{
	public Maybe(TType value) => (Value, HasValue) = (value, value is not null);

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

	public bool HasValue { get; init; }

	public static Maybe<TType> NoValue => new(default!);
	public TType? Value { get; init; }
}

public abstract record LinkBase
{
	public required string Name { get; set; }

}
public record ExternalLink : LinkBase
{
	public Uri? Uri { get; init; }
}
public record InternalLink : LinkBase
{
	public required string Identifier { get; init; }

}
public abstract record IdentifierBase
{
	public required string AssemblyName { get; init; }
	public required string Name { get; init; }
	public required string Namespace { get; init; }

	public string GetName()
	{
		return $"{AssemblyName}_{Namespace}_{Name}";

	}
}
public abstract record IdentifierBase<TParent> : IdentifierBase
{
	public TParent? Parent { get; init; } = default;
}

public record ClassIdentifier : IdentifierBase { }
public record InterfaceIdentifier : IdentifierBase { }
public record ClassMemberIdentifier : IdentifierBase<ClassIdentifier> { }
public record MethodIdentifier : ClassMemberIdentifier { }
public record FieldIdentifier : ClassMemberIdentifier { }
public record PropertyIdentifier : ClassMemberIdentifier { }
public record ConstructorIdentifier : ClassMemberIdentifier { }

public abstract record DocumentationMemberIdBase
{
	public required string FriendlyName { get; set; }

}
public abstract record DocumentationMemberIdBase<TLink> : DocumentationMemberIdBase where TLink : LinkBase
{
	public required TLink Link { get; set; }
}
public record InternalDocumentationMemberId : DocumentationMemberIdBase<InternalLink> { }
public record ExternalDocumentationMemberId : DocumentationMemberIdBase<ExternalLink> { }
public abstract class MemberDocumentationBase
{
	public required InternalDocumentationMemberId DocumentationMemberIdBase { get; set; }
	public string? DocumentationXML { get; set; }
	public string? DocumentationJson { get; set; }

}
public abstract class MemberDocumentation<TIdentifier> : MemberDocumentationBase
	where TIdentifier : IdentifierBase
{
	public required TIdentifier Identifier { get; set; }
	public string Type => Identifier.GetType().Name;
}
public abstract class TypeMemberDocumentation<TIdentifier> : MemberDocumentation<TIdentifier>
		where TIdentifier : IdentifierBase
{
	public required string Accessibility { get; set; }
	public required bool IsAbstract { get; set; }
	public required bool IsPartial { get; set; }
	public required bool IsSealed { get; set; }
	public required bool IsStatic { get; set; }
	public required bool IsVirtual { get; set; }
}
public class TypeDocumentation<TIdentifier> : MemberDocumentation<TIdentifier>
	where TIdentifier : IdentifierBase
{
	public void AddTypeMember(MemberDocumentationBase classMemberDocumentation)
	{
		TypeMembers.Add(classMemberDocumentation);
	}

	public TypeDocumentation<TIdentifier> WithMembers(List<MemberDocumentationBase> classMembers)
	{
		TypeMembers = classMembers;
		return this;
	}

	public List<MemberDocumentationBase> TypeMembers { get; set; } = new();
}

public class MethodDocumentation : TypeMemberDocumentation<MethodIdentifier>
{
	public List<(IdentifierBase type, string argumentName)> Arguments { get; set; } = new();
}
public class FieldDocumentation : TypeMemberDocumentation<FieldIdentifier>
{
	public required IdentifierBase FieldType { get; set; }
}
public class PropertyDocumentation : TypeMemberDocumentation<PropertyIdentifier>
{
	public required IdentifierBase PropertyType { get; set; }
}
public class ConstructorDocumentation : TypeMemberDocumentation<ConstructorIdentifier>
{
	public List<(IdentifierBase type, string argumentName)> Arguments { get; set; } = new();
}
public class ClassDocumentation : TypeDocumentation<ClassIdentifier> { }
public class InterfaceDocumentation : TypeDocumentation<InterfaceIdentifier> { }