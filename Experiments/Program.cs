using Weberknecht;

{
    var method = Method.Read(typeof(TestClass).GetMethod(nameof(TestClass.Print))!);
    Console.WriteLine(method.ToString(debugInfo: true));
}

{
    int b = 2;

    MyMethod original = (in a) =>
    {
        return a + b + TestClass.CreateInt();
    };

    var method = Method.Read(original);

    Console.WriteLine(method);

    var dynMethod = method.MakeDynamicMethod("Test");

    var dynDelegate = dynMethod.CreateDelegate<MyMethod>(original.Target);
    Console.WriteLine($"{original(42)} = {dynDelegate(42)}");
}

delegate int MyMethod(in int a);
