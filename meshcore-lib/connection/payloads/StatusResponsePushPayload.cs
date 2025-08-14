namespace meshcore_lib.connection.payloads;

public record StatusResponsePushPayload(
	byte Reserved,
	byte[] PubKeyPrefix,
	byte[] StatusData
) { }