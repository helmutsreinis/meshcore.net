namespace meshcore_lib.connection.payloads;

public record SentPayload(
	sbyte Result,
	uint ExpectedAckCrc,
	uint EstTimeout
){}