namespace Weberknecht;

internal readonly struct GenericContext
{
    private readonly Type[] _typeParams;
    private readonly Type[] _methodParams;

    public GenericContext(Type[] typeParams, int mvarCount)
    {
        _typeParams = typeParams;
        _methodParams = new Type[mvarCount];
        for (int i = 0; i < mvarCount; i++)
            _methodParams[i] = Type.MakeGenericMethodParameter(i);
    }

    public GenericContext(Type[] typeParams, Type[] methodParams)
    {
        _typeParams = typeParams;
        _methodParams = methodParams;
    }

    public Type GetTypeParameter(int index) => _typeParams[index];

    public Type GetMethodParameter(int index) => _methodParams[index];
}