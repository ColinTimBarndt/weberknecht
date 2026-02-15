using Weberknecht;

int b = 2;

var method = MethodReader.Read((in int a) =>
{
    int temp = a + b;
    unsafe
    {
        fixed (int* x = &a)
        {
            Console.WriteLine($"{(nint)x:016x} = {*x}");
        }
    }
    return temp + TestClass.CreateInt();
});

Console.WriteLine(method.ToString(debugInfo: true));
