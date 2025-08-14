namespace meshcore_lib.connection.payloads;

public record LoginSuccessPushPayload(byte Reserved, byte[] PubKeyPrefix) { }