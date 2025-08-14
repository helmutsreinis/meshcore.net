namespace meshcore_lib.connection.payloads;

public record SendConfirmedPushPayload(uint AckCode, uint RoundTrip) { }