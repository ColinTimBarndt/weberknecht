# Weberknecht

Complements the [System.Reflection] API with full IL inspection capabilities at runtime.

[System.Reflection]: https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/overview

## Usage

Everything is still work in progress.

```cs
int b = 2;

MethodReader.Read((int a) =>
{
  return a + b + TestClass.CreateInt();
});
```

Prints (Debug build):

```txt
  0000: nop
  0001: ldarg.1
  0002: ldarg.0
  0003: ldfld "b"
  0008: add
  0009: call Int32 CreateInt()
  000E: add
  000F: stloc.0
  0010: br.s 0012
  0012: ldloc.0
  0013: ret
```
