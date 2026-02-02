using Weberknecht;

Console.WriteLine("Hello, World!");

int b = 2;

MethodReader.Read((int a) =>
{
    return a + b + TestClass.CreateInt();
});
