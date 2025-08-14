namespace meshcore_lib.connection.payloads;

public record TraceDataPushPayload(
	byte Reserved,
	byte PathLen,
	byte Flags,
	uint Tag,
	uint AuthCode,
	byte[] PathHashes,
	byte[] PathSnrs,
	sbyte LastSnr
) { }