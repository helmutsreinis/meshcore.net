namespace meshcore_lib.connection.payloads;

public record RawDataPushPayload(sbyte LastSnr, sbyte LastRssi, byte Reserved, byte[] RemainingBytes) { }