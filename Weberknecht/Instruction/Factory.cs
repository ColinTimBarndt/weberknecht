using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht;

public partial struct Instruction
{

	public static Instruction Add(OverflowBehavior overflow = OverflowBehavior.Unchecked) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => OpCodes.Add_Ovf,
		OverflowBehavior.CheckUnsigned => OpCodes.Add_Ovf_Un,
		_ => OpCodes.Add,
	});

	public enum OverflowBehavior
	{
		Unchecked,
		CheckSigned,
		CheckUnsigned,
	}

	public static Instruction And() => new(OpCodes.And);

	public static Instruction ArgumentList() => new(OpCodes.Arglist);

	public static Instruction Branch(BranchCondition condition = BranchCondition.Always, bool isSigned = true, bool isShort = false) => new(condition switch
	{
		BranchCondition.Equal when isShort => OpCodes.Beq_S,
		BranchCondition.Equal => OpCodes.Beq,

		BranchCondition.NotEqual when isShort => OpCodes.Bne_Un_S,
		BranchCondition.NotEqual => OpCodes.Bne_Un,

		BranchCondition.Greater when isShort => isSigned ? OpCodes.Bgt_S : OpCodes.Bgt_Un_S,
		BranchCondition.Greater => isSigned ? OpCodes.Bgt : OpCodes.Bgt_Un,

		BranchCondition.GreaterOrEqual when isShort => isSigned ? OpCodes.Bge_S : OpCodes.Bge_Un_S,
		BranchCondition.GreaterOrEqual => isSigned ? OpCodes.Bge : OpCodes.Bge_Un,

		BranchCondition.Less when isShort => isSigned ? OpCodes.Blt_S : OpCodes.Blt_Un_S,
		BranchCondition.Less => isSigned ? OpCodes.Blt : OpCodes.Blt_Un,

		BranchCondition.LessOrEqual when isShort => isSigned ? OpCodes.Ble_S : OpCodes.Ble_Un_S,
		BranchCondition.LessOrEqual => isSigned ? OpCodes.Ble : OpCodes.Ble_Un,

		BranchCondition.False when isShort => OpCodes.Brfalse_S,
		BranchCondition.False => OpCodes.Brfalse,

		BranchCondition.True when isShort => OpCodes.Brtrue_S,
		BranchCondition.True => OpCodes.Brtrue,
		_ => isShort ? OpCodes.Br_S : OpCodes.Br,
	});

	public enum BranchCondition
	{
		Always,
		Equal,
		NotEqual,
		Greater,
		GreaterOrEqual,
		Less,
		LessOrEqual,
		False,
		True,
	}

	public static Instruction Box<T>() => Box(typeof(T));

	public static Instruction Box(Type type) => new(OpCodes.Box, type);

	public static Instruction Breakpoint() => new(OpCodes.Break);

	public static Instruction Call(Delegate method) => Call(method.Method);

	public static Instruction Call(MethodBase method) => new(OpCodes.Call, method);

	public static Instruction CallIndirect() => throw new NotImplementedException(); // TODO

	public static Instruction CallVirtual(MethodBase method)
	{
		if (!method.IsVirtual)
			throw new ArgumentException("Not a virtual method", nameof(method));
		return new(OpCodes.Callvirt, method);
	}

	public static Instruction CastClass<T>() => CastClass(typeof(T));

	public static Instruction CastClass(Type type) => new(OpCodes.Castclass, type);

	public static Instruction Compare(Comparison comparison, bool isSigned = true) => new(comparison switch
	{
		Comparison.Greater => isSigned ? OpCodes.Cgt : OpCodes.Cgt_Un,
		Comparison.Less => isSigned ? OpCodes.Clt : OpCodes.Clt_Un,
		_ => OpCodes.Ceq,
	});

	public enum Comparison
	{
		Equal,
		Greater,
		Less,
	}

	public static Instruction CheckFinite() => new(OpCodes.Ckfinite);

	public static Instruction ConvertNint(OverflowBehavior overflow = OverflowBehavior.Unchecked, bool isSigned = true) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => isSigned ? OpCodes.Conv_Ovf_I : OpCodes.Conv_Ovf_U,
		OverflowBehavior.CheckUnsigned => isSigned ? OpCodes.Conv_Ovf_I_Un : OpCodes.Conv_Ovf_U_Un,
		_ => isSigned ? OpCodes.Conv_I : OpCodes.Conv_U,
	});

	public static Instruction ConvertByte(OverflowBehavior overflow = OverflowBehavior.Unchecked, bool isSigned = true) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => isSigned ? OpCodes.Conv_Ovf_I1 : OpCodes.Conv_Ovf_U1,
		OverflowBehavior.CheckUnsigned => isSigned ? OpCodes.Conv_Ovf_I1_Un : OpCodes.Conv_Ovf_U1_Un,
		_ => isSigned ? OpCodes.Conv_I1 : OpCodes.Conv_U1,
	});

	public static Instruction ConvertShort(OverflowBehavior overflow = OverflowBehavior.Unchecked, bool isSigned = true) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => isSigned ? OpCodes.Conv_Ovf_I2 : OpCodes.Conv_Ovf_U2,
		OverflowBehavior.CheckUnsigned => isSigned ? OpCodes.Conv_Ovf_I2_Un : OpCodes.Conv_Ovf_U2_Un,
		_ => isSigned ? OpCodes.Conv_I2 : OpCodes.Conv_U2,
	});

	public static Instruction ConvertInt(OverflowBehavior overflow = OverflowBehavior.Unchecked, bool isSigned = true) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => isSigned ? OpCodes.Conv_Ovf_I4 : OpCodes.Conv_Ovf_U4,
		OverflowBehavior.CheckUnsigned => isSigned ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_U4_Un,
		_ => isSigned ? OpCodes.Conv_I4 : OpCodes.Conv_U4,
	});

	public static Instruction ConvertLong(OverflowBehavior overflow = OverflowBehavior.Unchecked, bool isSigned = true) => new(overflow switch
	{
		OverflowBehavior.CheckSigned => isSigned ? OpCodes.Conv_Ovf_I8 : OpCodes.Conv_Ovf_U8,
		OverflowBehavior.CheckUnsigned => isSigned ? OpCodes.Conv_Ovf_I8_Un : OpCodes.Conv_Ovf_U8_Un,
		_ => isSigned ? OpCodes.Conv_I8 : OpCodes.Conv_U8,
	});

	public static Instruction ConvertFloat(bool isSigned = true) => new(isSigned ? OpCodes.Conv_R4 : OpCodes.Conv_R_Un);

	public static Instruction ConvertDouble() => new(OpCodes.Conv_R8);

	public static Instruction CopyBulk() => new(OpCodes.Cpblk);

	public static Instruction CopyObject<T>() where T : struct => new(OpCodes.Cpobj, typeof(T));

	public static Instruction CopyObject(Type type)
	{
		if (!type.IsValueType)
			throw new ArgumentException("Not a value type", nameof(type));
		return new(OpCodes.Cpobj, type);
	}

	public static Instruction Divide(bool isSigned = true) => new(isSigned ? OpCodes.Div : OpCodes.Div_Un);

	public static Instruction Duplicate() => new(OpCodes.Dup);

	// TODO

	public static Instruction Load(string value) => new(OpCodes.Ldstr, value);

	public static Instruction Load(uint value) => Load((int)value);

	public static Instruction Load(int value)
	{
		sbyte shortValue = (sbyte)value;
		if (shortValue == value)
		{
			var opCode = shortValue switch
			{
				-1 => OpCodes.Ldc_I4_M1,
				0 => OpCodes.Ldc_I4_0,
				1 => OpCodes.Ldc_I4_1,
				2 => OpCodes.Ldc_I4_2,
				3 => OpCodes.Ldc_I4_3,
				4 => OpCodes.Ldc_I4_4,
				5 => OpCodes.Ldc_I4_5,
				6 => OpCodes.Ldc_I4_6,
				7 => OpCodes.Ldc_I4_7,
				8 => OpCodes.Ldc_I4_8,
				_ => OpCodes.Ldc_I4_S
			};
			if (opCode == OpCodes.Ldc_I4_S)
				return new(opCode, shortValue);
			return new(opCode);
		}

		return new(OpCodes.Ldc_I4, value);
	}

	public static Instruction Load(ulong value) => Load((long)value);

	public static Instruction Load(long value) => new(OpCodes.Ldc_I8, value);

	public static Instruction Load(float value) => new(OpCodes.Ldc_R4, value);

	public static Instruction Load(double value) => new(OpCodes.Ldc_R8, value);

	public static Instruction LoadArgument(ushort index)
	{
		if ((index & ~0xff) == 0)
		{
			byte shortIndex = (byte)index;
			var opCode = shortIndex switch
			{
				0 => OpCodes.Ldarg_0,
				1 => OpCodes.Ldarg_1,
				2 => OpCodes.Ldarg_2,
				3 => OpCodes.Ldarg_3,
				_ => OpCodes.Ldarg_S,
			};
			if (opCode == OpCodes.Ldarg_S)
				return new(opCode, shortIndex);
			return new(opCode);
		}
		return new(OpCodes.Ldarg, index);
	}

	public static Instruction LoadArgumentAddress(ushort index)
	{
		if ((index & ~0xff) == 0)
			return new(OpCodes.Ldarga_S, (byte)index);
		return new(OpCodes.Ldarga, index);
	}

	public static Instruction LoadNull() => new(OpCodes.Ldnull);

	public static Instruction LoadToken(Delegate method) => LoadToken(method.Method);

	public static Instruction LoadToken(MethodBase method) => new(OpCodes.Ldtoken, method);

	public static Instruction LoadToken(FieldInfo field) => new(OpCodes.Ldtoken, field);

	public static Instruction LoadToken<T>() => LoadToken(typeof(T));

	public static Instruction LoadToken(Type type) => new(OpCodes.Ldtoken, type);

	public static Instruction LoadField(FieldInfo field) => new(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);

	public static Instruction LoadFieldAddress(FieldInfo field) => new(field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, field);

	// TODO

	public static Instruction NewObject<T>() where T : new()
		=> NewObject(typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)!);

	public static Instruction NewObject(ConstructorInfo ctor) => new(OpCodes.Newobj, ctor);

	// TODO

	public static Instruction Return() => new(OpCodes.Ret);

}