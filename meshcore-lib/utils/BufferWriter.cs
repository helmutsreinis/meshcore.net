using System.Text;
// ReSharper disable InconsistentNaming

namespace meshcore_lib.utils;

/// <summary>
/// Binary writer utility for constructing Meshcore protocol frames.
/// Provides helper methods for writing various data types in the correct format.
/// </summary>
/// <remarks>
/// This class extends BinaryWriter with Meshcore-specific methods for writing
/// protocol data in little-endian format and handling special types like C-style strings.
/// </remarks>
/// <example>
/// <code>
/// using var ms = new MemoryStream();
/// using var writer = new BufferWriter(ms);
/// writer.WriteByte(Constants.CommandCodes.AppStart);
/// writer.WriteUInt32LE(timestamp);
/// writer.WriteString("Hello");
/// byte[] frame = ms.ToArray();
/// </code>
/// </example>
public class BufferWriter(Stream stream) : BinaryWriter(stream) {
	/// <summary>
	/// Writes a single byte to the stream.
	/// </summary>
	/// <param name="b">The byte value to write</param>
	public void WriteByte(byte b) {
		Write(b);
	}

	/// <summary>
	/// Writes a command code enum value as a byte.
	/// </summary>
	/// <param name="code">The command code to write</param>
	public void WriteByte(Constants.CommandCodes code) {
		WriteByte(Constants.GetCommandCodeByte(code));
	}
	
	/// <summary>
	/// Writes a text type enum value as a byte.
	/// </summary>
	/// <param name="txtType">The text type to write</param>
	public void WriteByte(Constants.TxtType txtType) {
		WriteByte(Constants.GetTxtTypeByte(txtType));
	}
	
	/// <summary>
	/// Writes an advertisement type enum value as a byte.
	/// </summary>
	/// <param name="advType">The advertisement type to write</param>
	public void WriteByte(Constants.AdvType advType) {
		WriteByte(Constants.GetAdvTypeByte(advType));
	}
	
	/// <summary>
	/// Writes a self-advertisement type enum value as a byte.
	/// </summary>
	/// <param name="selfAdvertType">The self-advertisement type to write</param>
	public void WriteByte(Constants.SelfAdvertTypes selfAdvertType) {
		WriteByte(Constants.GetSelfAdvertByte(selfAdvertType));
	}
	
	/// <summary>
	/// Writes a byte array to the stream.
	/// </summary>
	/// <param name="bytes">The bytes to write</param>
	public void WriteBytes(byte[] bytes) {
		foreach (byte b in bytes) {
			WriteByte(b);
		}
	}

	/// <summary>
	/// Writes a UTF-8 encoded string to the stream.
	/// </summary>
	/// <param name="s">The string to write</param>
	/// <remarks>Does not write length prefix or null terminator</remarks>
	public void WriteString(string s) {
		WriteBytes(Encoding.UTF8.GetBytes(s));
	}

	/// <summary>
	/// Writes an unsigned 16-bit integer in little-endian format.
	/// </summary>
	/// <param name="num">The value to write</param>
	public void WriteUInt16LE(ushort num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
	}

	/// <summary>
	/// Writes an unsigned 32-bit integer in little-endian format.
	/// </summary>
	/// <param name="num">The value to write</param>
	public void WriteUInt32LE(uint num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
		WriteByte((byte) (num >> 16));
		WriteByte((byte) (num >> 24));
	}

	/// <summary>
	/// Writes a signed 32-bit integer in little-endian format.
	/// </summary>
	/// <param name="num">The value to write</param>
	public void WriteInt32LE(int num) {
		WriteByte((byte) num);
		WriteByte((byte) (num >> 8));
		WriteByte((byte) (num >> 16));
		WriteByte((byte) (num >> 24));
	}
	
	/// <summary>
	/// Writes a fixed-length C-style null-terminated string.
	/// </summary>
	/// <param name="s">The string to write</param>
	/// <param name="maxLength">The maximum length including null terminator</param>
	/// <remarks>
	/// The string is truncated to maxLength-1 characters and padded with zeros.
	/// A null terminator is always written at position maxLength.
	/// </remarks>
	public void WriteCString(string s, int maxLength) {
		byte[] res = new byte[maxLength];
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		
		Buffer.BlockCopy(bytes, 0, res, 0, maxLength-1);
		res[maxLength] = 0;
		
		WriteBytes(res);
	}
}