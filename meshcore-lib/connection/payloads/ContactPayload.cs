namespace meshcore_lib.connection.payloads;

public record ContactPayload(
	byte[] PublicKey,
	byte Type,
	byte Flags,
	sbyte OutPathLen,
	byte[] OutPath,
	string AdvName,
	uint LastAdvert,
	uint AdvLat,
	uint AdvLon,
	uint LastMod
) { }