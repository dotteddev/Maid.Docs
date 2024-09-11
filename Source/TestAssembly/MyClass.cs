namespace TestAssembly;

/// <summary>
/// My test class
/// </summary>
/// <trackingId>44de27be-135a-4712-b36d-f7dbcdc6290a</trackingId>
public class MyClass : MyBaseClass, IMyInterface
{
	/// <summary>
	/// my test field
	/// </summary>
	/// <trackingId>b25c3fc8-65e2-4008-8ca8-d17fae7f0355</trackingId>
	private readonly string _myField;

	/// <summary>
	/// constructor
	/// </summary> 
	/// <param name="myField">my field</param> 
	/// <trackingId></trackingId>
	public MyClass(string myField)
	{

		_myField = myField;
		Console.WriteLine();
	}
	/// <summary>
	/// 
	/// </summary>
	/// <trackingId>2bbb48c1-bc60-4606-b5da-82251dcefb7e</trackingId>
	public string MyField { get; set; } = string.Empty;

	/// <summary>
	/// my method
	/// </summary>
	/// <returns>tato metoda vrací<see cref="string"/>, takze cajk</returns>
	/// <trackingId>38bd7f1a-3959-41df-8dde-7e5639043dc2</trackingId>
	public string GetMyField()
	{
		if (true) return string.Empty;
	}
}
/// <summary>
/// 
/// </summary>
/// <trackingId>a11521bd-2173-4b34-8945-e369124449e6</trackingId>
public class MyBaseClass
{

}
/// <summary>
/// 
/// </summary>
/// <trackingId>91f1ded8-6ef2-497e-b339-a301857cf904</trackingId>
public interface IMyInterface
{
}