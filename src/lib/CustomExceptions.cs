using System;
using System.Runtime.Serialization;

/*\
You can override as many constructors and methods as you want(or need) to.
In order to create new Exception types just add new one-liner classes:

public class MyCustomException : Exception { }
public class SomeOtherException : Exception { }

If you want to raise your custom exception use:

throw new CustomException<MyCustomException>("your error description");

This keeps your Exception code simple and allows you to distinguish between those exceptions:

try
{
    // ...
}
catch(CustomException<MyCustomException> ex)
{
    // handle your custom exception ...
}
catch(CustomException<SomeOtherException> ex)
{
    // handle your other exception ...
}
\*/

namespace Core
{	
	public class ExceptionBadSize : Exception { }
	public class ExceptionWrongName : Exception { }
	public class ExceptionWrongExtension : Exception { }
	public class ExceptionBadSignature : Exception { }

	public class CustomException<T> : Exception where T : Exception
	{
		public object argument;
		public CustomException() { }
		public CustomException(string message, object arg) : base(message) { argument = arg; }
		public CustomException(string message) : base(message) { }
		public CustomException(string message, Exception innerException) : base(message, innerException) { }
		public CustomException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}
}
