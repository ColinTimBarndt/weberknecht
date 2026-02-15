using Weberknecht;

var method = MethodReader.Read(typeof(TestClass).GetMethod(nameof(TestClass.Print))!);

Console.WriteLine(method.ToString(debugInfo: true));
