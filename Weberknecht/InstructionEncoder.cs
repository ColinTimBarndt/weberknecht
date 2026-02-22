using System.Buffers.Binary;
using System.Reflection;
using System.Reflection.Emit;
using Weberknecht.Metadata;

namespace Weberknecht;

internal ref struct InstructionEncoder<T>(Span<byte> buffer, T tokens)
where T : ITokenSource
{

	private readonly Span<byte> _buffer = buffer;
	private readonly T _tokens = tokens;

	private int _index = 0;
	private readonly List<(bool, int, Label)> _writeLabels = [];

	public readonly int CurrentAddress => _index;

	public void Emit(Instruction instruction)
	{
		bool small = (ushort)instruction.OpCode.Value <= 255;
		if (small)
			_buffer[_index] = (byte)instruction.OpCode.Value;
		else
			BinaryPrimitives.WriteInt16BigEndian(_buffer[_index..], instruction.OpCode.Value);

		_index += small ? 1 : 2;

		switch (instruction.OpCode.OperandType)
		{
			case OperandType.InlineBrTarget:
				WriteLabel((Label)instruction._uoperand.@int, false);
				return;

			case OperandType.ShortInlineBrTarget:
				WriteLabel((Label)instruction._uoperand.@int, true);
				return;

			case OperandType.InlineField:
				WriteInt32(_tokens.GetToken((FieldInfo)instruction._operand!));
				return;

			case OperandType.InlineI:
				WriteInt32(instruction._uoperand.@int);
				return;

			case OperandType.InlineI8:
			case OperandType.ShortInlineI:
			case OperandType.ShortInlineVar:
				WriteUInt8(instruction._uoperand.@byte);
				return;

			case OperandType.InlineMethod:
				WriteInt32(_tokens.GetToken((MethodBase)instruction._operand!));
				return;

			case OperandType.InlineNone:
				return;

			case OperandType.InlineR:
				WriteDouble(instruction._uoperand.@double);
				return;

			case OperandType.InlineSig:
				throw new NotImplementedException("InlineSig"); // TODO

			case OperandType.InlineString:
				WriteInt32(_tokens.GetToken((string)instruction._operand!));
				return;

			case OperandType.InlineSwitch:
				throw new NotImplementedException("switch");

			case OperandType.InlineTok:
				WriteInt32(instruction._operand switch
				{
					FieldInfo field => _tokens.GetToken(field),
					MethodInfo method => _tokens.GetToken(method),
					ConstructorInfo ctor => _tokens.GetToken(ctor),
					Type type => _tokens.GetToken(type),
					_ => throw new NotImplementedException($"InlineTok {instruction._operand?.GetType()}"),
				});
				return;

			case OperandType.InlineType:
				WriteInt32(_tokens.GetToken((Type)instruction._operand!));
				return;

			case OperandType.InlineVar:
				WriteUint16(instruction._uoperand.@ushort);
				return;

			case OperandType.ShortInlineR:
				WriteSingle(instruction._uoperand.@float);
				return;

			default:
				throw new NotImplementedException($"OperandType {Enum.GetName(instruction.OpCode.OperandType)}");
		}
	}

	public readonly void WriteLabels(LabelAddressMap labels)
	{
		foreach (var (isShort, index, label) in _writeLabels)
		{
			int offset; // address of next instruction which the jump is relative to
			if (isShort)
			{
				offset = index + 1;
				_buffer[index] = (byte)(labels[label] - offset);
			}
			else
			{
				offset = index + 4;
				BinaryPrimitives.WriteInt32LittleEndian(_buffer[index..], labels[label] - offset);
			}
		}
		_writeLabels.Clear();
	}

	private void WriteLabel(Label label, bool isShort)
	{
		_writeLabels.Add((isShort, _index, label));
		_index += isShort ? 1 : 4;
	}

	private void WriteUInt8(byte value)
	{
		_buffer[_index++] = value;
	}

	private void WriteUint16(ushort value)
	{
		BinaryPrimitives.WriteUInt16LittleEndian(_buffer[_index..], value);
		_index += 2;
	}

	private void WriteInt32(int value)
	{
		BinaryPrimitives.WriteInt32LittleEndian(_buffer[_index..], value);
		_index += 4;
	}

	private void WriteSingle(float value)
	{
		BinaryPrimitives.WriteSingleLittleEndian(_buffer[_index..], value);
		_index += 4;
	}

	private void WriteDouble(double value)
	{
		BinaryPrimitives.WriteDoubleLittleEndian(_buffer[_index..], value);
		_index += 8;
	}

}