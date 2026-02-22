using Weberknecht;

{
    Func<int, int, int> original = static (int a, int b) =>
    {
        int c = a + b;
        try
        {
            if (c > 0)
                throw new Exception();
            return c;
        }
        catch
        {
            return 42;
        }
    };

    var method = Method.Read(original);
    Console.WriteLine(method);

    var dynMethod = method.MakeDynamicMethod2("Add");

    var dynDelegate = dynMethod.CreateDelegate<Func<int, int, int>>();
    Console.WriteLine($"{original(1, 2)} = {dynDelegate(1, 2)}");
}

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

    var dynMethod = method.MakeDynamicMethod2("Test");

    var dynDelegate = dynMethod.CreateDelegate<MyMethod>(original.Target);
    Console.WriteLine($"{original(42)} = {dynDelegate(42)}");
}

{
    var method = Method.Read(() =>
    {
        try
        {
            Console.WriteLine();
        }
        catch (AggregateException)
        {
            Console.WriteLine("AggregateException");
        }
        catch (ArgumentException e) when (e.Message.Length > 0)
        {
            Console.WriteLine("ArgumentException");
        }
        catch
        {
            Console.WriteLine("Other");
        }
        finally
        {
            Console.WriteLine("Finally");
        }
    });
    Console.WriteLine(method);
    method.MakeDynamicMethod2("Test");
}

delegate int MyMethod(in int a);