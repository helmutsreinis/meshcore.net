namespace meshcore_lib.connection.payloads;

public record DeviceInfoPayload(
	sbyte FirmwareVer,
	byte[] Reserved,
	string FirmwareBuildDate,
	string ManufacturerModel
){}