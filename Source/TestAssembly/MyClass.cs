namespace TestAssembly;

/// <summary>
/// My test class
/// </summary>
public class MyClass : MyBaseClass, IMyInterface
{
	/// <summary>
	/// my test field
	/// </summary>
	readonly string _myField;

	/// <summary>
	/// constructor
	/// </summary>
	/// <param name="myField">my field</param>
	public MyClass(string myField)
	{
		_myField = myField;
	}

	public string MyField { get; set; } = string.Empty;

	/// <summary>
	/// my method
	/// </summary>
	/// <returns></returns>
	public string GetMyField()
	{
		return _myField;
	}
}

public class MyBaseClass
{

}

public interface IMyInterface
{
}