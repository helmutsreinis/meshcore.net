namespace meshcore_lib.connection.payloads;

public record ChannelInfoPayload(
	byte ChannelIdx,
	string Name,
	byte[] Secret
) { }