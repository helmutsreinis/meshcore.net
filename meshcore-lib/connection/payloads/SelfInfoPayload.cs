namespace meshcore_lib.connection.payloads;

public record SelfInfoPayload(
	byte Type,
	byte TxPower,
	byte MaxTxPower,
	byte[] PublicKey,
	int AdvLat,
	int AdvLon,
	byte[] Reserved,
	byte MadualAddContacts,
	uint RadioFreq,
	uint RadioBw,
	byte RadioSf,
	byte RadioCr,
	string name
) { }