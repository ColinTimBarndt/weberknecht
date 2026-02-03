using Weberknecht;

Console.WriteLine("Hello, World!");

int b = 2;

var method = MethodReader.Read((in int a) =>
{
    return a + b + TestClass.CreateInt();
});

Console.WriteLine(method);
