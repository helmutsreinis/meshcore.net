using System.Text;

namespace meshcore_lib.utils;

public class BufferReader(Stream s) : BinaryReader(s) {
	public byte[] ReadRemainingBytes() {
		List<byte> bytes = new();
		while (true) {
			try {
				bytes.Add(ReadByte());
			}
			catch (EndOfStreamException) {
				break;
			}
		}

		return bytes.ToArray();
	}

	public uint ReadUInt32LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		byte b3 = ReadByte();
		byte b4 = ReadByte();
		return (uint)(b1 | (b2 << 8) | (b3 << 16) | (b4 << 24));
	}

	public ushort ReadUInt16LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		return (ushort)(b1 | (b2 << 8));
	}
	
	public short ReadInt16LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		return (short)(b1 | (b2 << 8));
	}
	
	public string ReadCString(int maxLength) {
		byte[] bytes = ReadBytes(maxLength);
		int nullTerminator = Array.IndexOf(bytes, (byte)0);
		if (nullTerminator >= 0) {
			return Encoding.UTF8.GetString(bytes, 0, nullTerminator);
		}
		return Encoding.UTF8.GetString(bytes);
	}

	public int ReadInt32LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		byte b3 = ReadByte();
		byte b4 = ReadByte();
		return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
	}
}