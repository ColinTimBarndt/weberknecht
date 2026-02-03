# Weberknecht

Complements the [System.Reflection] API with full IL inspection capabilities at runtime.

[System.Reflection]: https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/overview

## Usage

Everything is still work in progress.

```cs
int b = 2;

var method = MethodReader.Read((in int a) =>
{
    return a + b + TestClass.CreateInt();
});

Console.WriteLine(method);
```

Prints (Debug build):

```txt
Int32 Method(in Int32& a)
        nop
        ldarg.1
        ldind.i4
        ldarg.0
        ldfld Int32 b
        add
        call Int32 CreateInt()
        add
        stloc.0
        br.s L0001
L0001:  ldloc.0
        ret
```
