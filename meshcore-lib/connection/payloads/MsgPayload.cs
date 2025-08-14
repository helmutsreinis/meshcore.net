namespace meshcore_lib.connection.payloads;

public record MsgPayload(
	string source,
	byte PathLen,
	byte TxtType,
	uint SenderTimestamp,
	string Text,
	bool IsContact
) {
    
}