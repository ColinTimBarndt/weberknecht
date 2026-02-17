using System.Reflection.Emit;
using Weberknecht;

{
    var method = MethodReader.Read(typeof(TestClass).GetMethod(nameof(TestClass.Print))!);
    Console.WriteLine(method.ToString(debugInfo: true));
}

{
    int b = 2;

    MyMethod original = (in int a) =>
    {
        return a + b + TestClass.CreateInt();
    };

    var method = MethodReader.Read(original);

    Console.WriteLine(method);

    var dynMethod = method.MakeDynamicMethod("Test");

    var dynDelegate = dynMethod.CreateDelegate<MyMethod>(original.Target);
    Console.WriteLine($"{original(42)} = {dynDelegate(42)}");
}

delegate int MyMethod(in int a);
