![build workflow status](https://github.com/ColinTimBarndt/weberknecht/actions/workflows/build.yml/badge.svg?branch=main)

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

### A more complex example

```cs
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
```
Release build output:
```txt
Int32 Method(in Int32& a)
.locals (Int32* x, pinned Int32&, DefaultInterpolatedStringHandler)
  @ _/Experiments/Program.cs:7:5
        ldarg.1
        ldind.i4
        ldarg.0
        ldfld Int32 b
        add
  @ _/Experiments/Program.cs:9:5
        ldarg.1
        stloc.1
  @ _/Experiments/Program.cs:10:16
        ldloc.1
        conv.u
        stloc.0
  @ _/Experiments/Program.cs:12:13
        ldloca.s 2
        ldc.i4.3
        ldc.i4.2
        call Void .ctor(Int32, Int32)
        ldloca.s 2
        ldloc.0
        ldstr "016x"
        call Void AppendFormatted[IntPtr](IntPtr, System.String)
        ldloca.s 2
        ldstr " = "
        call Void AppendLiteral(System.String)
        ldloca.s 2
        ldloc.0
        ldind.i4
        call Void AppendFormatted[Int32](Int32)
        ldloca.s 2
        call System.String ToStringAndClear()
        call Void WriteLine(System.String)
  @ _/Experiments/Program.cs:<hidden>
        ldc.i4.0
        conv.u
        stloc.1
  @ _/Experiments/Program.cs:15:5
        call Int32 CreateInt()
        add
        ret
```
