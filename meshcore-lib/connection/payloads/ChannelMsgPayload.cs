namespace meshcore_lib.connection.payloads;

public record ChannelMsgPayload(
	sbyte ChannelIdx,
	byte PathLen,
	byte TxtType,
	uint SenderTimestamp,
	string Text
) {
	public MsgPayload Generalise() {
		return new MsgPayload(
			ChannelIdx + "",
			PathLen,
			TxtType,
			SenderTimestamp,
			Text,false
		);
	}
}