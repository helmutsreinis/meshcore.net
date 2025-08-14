using System.Text;
// ReSharper disable InconsistentNaming

namespace meshcore_lib.utils;

public class BufferWriter(Stream stream) : BinaryWriter(stream) {
	public void WriteByte(byte b) {
		Write(b);
	}

	public void WriteByte(Constants.CommandCodes code) {
		WriteByte(Constants.GetCommandCodeByte(code));
	}
	public void WriteByte(Constants.TxtType txtType) {
		WriteByte(Constants.GetTxtTypeByte(txtType));
	}
	public void WriteByte(Constants.AdvType advType) {
		WriteByte(Constants.GetAdvTypeByte(advType));
	}
	
	public void WriteByte(Constants.SelfAdvertTypes selfAdvertType) {
		WriteByte(Constants.GetSelfAdvertByte(selfAdvertType));
	}
	
	public void WriteBytes(byte[] bytes) {
		foreach (byte b in bytes) {
			WriteByte(b);
		}
	}

	public void WriteString(string s) {
		WriteBytes(Encoding.UTF8.GetBytes(s));
	}

	
	public void WriteUInt16LE(ushort num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
	}

	public void WriteUInt32LE(uint num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
		WriteByte((byte) (num >> 16));
		WriteByte((byte) (num >> 24));
	}

	public void WriteInt32LE(int num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
		WriteByte((byte) (num >> 16));
		WriteByte((byte) (num >> 24));
	}
	
	public void WriteCString(string s, int maxLength) {
		byte[] res = new byte[maxLength];
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		
		Buffer.BlockCopy(bytes, 0, res, 0, maxLength-1);
		res[maxLength] = 0;
		
		WriteBytes(res);
	}
}