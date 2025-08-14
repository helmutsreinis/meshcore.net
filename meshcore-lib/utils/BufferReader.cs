namespace meshcore_lib.utils;

public class BufferReader(Stream s) : BinaryReader(s) {
	public byte[] ReadRemainingBytes() {
		List<byte> bytes = new();
		while (true) {
			try {
				bytes.Add(ReadByte());
			}
			catch (EndOfStreamException e) {
				break;
			}
		}

		return bytes.ToArray();
	}

	public uint ReadUInt32LE() {
		throw new NotImplementedException();
	}

	public ushort ReadUInt16LE() {
		throw new NotImplementedException();
	}
	
	public string ReadCString(int i) {
		throw new NotImplementedException();
	}

	public int ReadInt32LE() {
		throw new NotImplementedException();
	}
}