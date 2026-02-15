static class TestClass
{
    public static int MyInt = 10;

    public static int CreateInt() => 42;

    public static void Print<T0>(T0 arg)
    {
        var temp = arg;
        DoSomething(temp);
        TestClassGeneric<T0>.DoSomething(temp);
        Console.WriteLine(temp);
    }

    public static void DoSomething<T1>(T1 _) { }
}

static class TestClassGeneric<T2>
{
    public static void DoSomething(T2 _) { }
}