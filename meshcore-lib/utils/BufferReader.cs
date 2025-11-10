using System.Text;

namespace meshcore_lib.utils;

/// <summary>
/// Binary reader utility for parsing Meshcore protocol frames.
/// Provides helper methods for reading various data types from incoming frames.
/// </summary>
/// <remarks>
/// This class extends BinaryReader with Meshcore-specific methods for reading
/// protocol data in little-endian format and handling special types like C-style strings.
/// </remarks>
/// <example>
/// <code>
/// using var ms = new MemoryStream(frameData);
/// using var reader = new BufferReader(ms);
/// byte responseCode = reader.ReadByte();
/// uint timestamp = reader.ReadUInt32LE();
/// string name = reader.ReadCString(32);
/// </code>
/// </example>
public class BufferReader(Stream s) : BinaryReader(s) {
	/// <summary>
	/// Reads all remaining bytes from the stream until end-of-stream is reached.
	/// </summary>
	/// <returns>A byte array containing all remaining data</returns>
	/// <remarks>Useful for reading variable-length payloads at the end of a frame</remarks>
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

	/// <summary>
	/// Reads an unsigned 32-bit integer in little-endian format.
	/// </summary>
	/// <returns>The read integer value</returns>
	public uint ReadUInt32LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		byte b3 = ReadByte();
		byte b4 = ReadByte();
		return (uint)(b1 | (b2 << 8) | (b3 << 16) | (b4 << 24));
	}

	/// <summary>
	/// Reads an unsigned 16-bit integer in little-endian format.
	/// </summary>
	/// <returns>The read integer value</returns>
	public ushort ReadUInt16LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		return (ushort)(b1 | (b2 << 8));
	}
	
	/// <summary>
	/// Reads a signed 16-bit integer in little-endian format.
	/// </summary>
	/// <returns>The read integer value</returns>
	public short ReadInt16LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		return (short)(b1 | (b2 << 8));
	}
	
	/// <summary>
	/// Reads a fixed-length C-style null-terminated string.
	/// </summary>
	/// <param name="maxLength">The maximum number of bytes to read</param>
	/// <returns>The decoded UTF-8 string, truncated at the first null byte if present</returns>
	/// <remarks>
	/// Reads exactly maxLength bytes but only decodes up to the first null terminator.
	/// This matches the C-style string format used in the protocol.
	/// </remarks>
	public string ReadCString(int maxLength) {
		byte[] bytes = ReadBytes(maxLength);
		int nullTerminator = Array.IndexOf(bytes, (byte)0);
		if (nullTerminator >= 0) {
			return Encoding.UTF8.GetString(bytes, 0, nullTerminator);
		}
		return Encoding.UTF8.GetString(bytes);
	}

	/// <summary>
	/// Reads a signed 32-bit integer in little-endian format.
	/// </summary>
	/// <returns>The read integer value</returns>
	public int ReadInt32LE() {
		byte b1 = ReadByte();
		byte b2 = ReadByte();
		byte b3 = ReadByte();
		byte b4 = ReadByte();
		return b1 | (b2 << 8) | (b3 << 16) | (b4 << 24);
	}
}