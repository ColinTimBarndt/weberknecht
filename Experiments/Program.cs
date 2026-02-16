using Weberknecht;

{
    var method = MethodReader.Read(typeof(TestClass).GetMethod(nameof(TestClass.Print))!);
    Console.WriteLine(method.ToString(debugInfo: true));
}

{
    int b = 2;

    var method = MethodReader.Read((in int a) =>
    {
        return a + b + TestClass.CreateInt();
    });

    Console.WriteLine(method);
}
