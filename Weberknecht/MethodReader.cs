using System.Reflection;

namespace Weberknecht;

public static class MethodReader
{

    public static void Read(Delegate d) => Read(d.Method);

    public static void Read(MethodInfo method)
    {
        var body = method.GetMethodBody()
            ?? throw new InvalidOperationException("Method has no available body");
        var assembly = method.Module.Assembly;
        var metadata = assembly.GetMetadataReader()
            ?? throw new InvalidOperationException("Cannot read assembly metadata");
        var ilBytes = body.GetILAsByteArray()
            ?? throw new InvalidOperationException("Method has no body");

        var types = new TypeResolver(assembly);

        var il = new InstructionDecoder(ilBytes, metadata, types);
        while (il.MoveNext())
        {
            Console.WriteLine($"  {il.CurrentAddress:X4}: {il.Current.ToString(metadata)}");
        }
    }

}