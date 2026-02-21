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

{
    Delegate method = () =>
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
    };

    Console.WriteLine(Method.Read(method));
}

delegate int MyMethod(in int a);
