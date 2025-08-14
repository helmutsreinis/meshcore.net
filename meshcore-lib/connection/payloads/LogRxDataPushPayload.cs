namespace meshcore_lib.connection.payloads;

public record LogRxDataPushPayload(
	sbyte LastSnr,
	sbyte LastRssi,
	byte[] Raw
) { }