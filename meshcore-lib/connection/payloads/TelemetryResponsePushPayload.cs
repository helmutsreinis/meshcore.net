namespace meshcore_lib.connection.payloads;

public record TelemetryResponsePushPayload(
	byte Reserved,
	byte[] PubKeyPrefix,
	byte[] LppSensorData
) { }