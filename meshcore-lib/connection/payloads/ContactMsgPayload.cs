namespace meshcore_lib.connection.payloads;

public record ContactMsgPayload(
	byte[] PubKeyPrefix,
	byte PathLen,
	byte TxtType,
	uint SenderTimestamp,
	string Text
) {
	public MsgPayload Generalise() {
		return new MsgPayload(
			Convert.ToBase64String(PubKeyPrefix),
			PathLen,
			TxtType,
			SenderTimestamp,
			Text,true
		);
	}
}