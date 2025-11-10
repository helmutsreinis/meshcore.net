# Meshcore.NET API Quick Reference

## Table of Contents
- [Connection Setup](#connection-setup)
- [Device Operations](#device-operations)
- [Contact Operations](#contact-operations)
- [Messaging Operations](#messaging-operations)
- [Channel Operations](#channel-operations)
- [Path Operations](#path-operations)
- [Time Operations](#time-operations)
- [Advanced Operations](#advanced-operations)
- [Events Reference](#events-reference)

---

## Connection Setup

### Create Custom Connection
```csharp
public class MyConnection : Connection
{
    protected override void Close() { /* cleanup */ }
    protected override void SendToRadioFrame(byte[] data) { /* send */ }
}
```

### Initialize
```csharp
var conn = new MyConnection();
conn.Connected += (s, e) => Console.WriteLine("Connected!");
conn.onConnected(); // Call when transport is ready
```

---

## Device Operations

### Get Device Info
```csharp
SelfInfoPayload info = conn.getSelfInfo();
// Returns: name, public key, location, radio config, etc.
```

### Set Device Name
```csharp
conn.setAdvertName("My Device");
```

### Set Location
```csharp
conn.setAdvertLatLong(latitude, longitude); // In microdegrees
```

### Configure Radio
```csharp
conn.setRadioParams(
    radioFreq: 915000000,  // Frequency in Hz
    radioBw: 125000,       // Bandwidth in Hz
    radioSf: 7,            // Spreading factor
    radioCr: 5             // Coding rate
);
```

### Set TX Power
```csharp
conn.setTxPower(20); // Power level (2-20)
```

### Get Battery
```csharp
conn.BatteryVoltageResponse += (s, mV) =>
{
    Console.WriteLine($"Battery: {mV}mV");
};
conn.sendCommandGetBatteryVoltage();
```

### Reboot Device
```csharp
conn.sendCommandReboot();
```

---

## Contact Operations

### Get All Contacts
```csharp
List<ContactPayload> contacts = conn.getContacts();
```

### Find Contact
```csharp
// By name
ContactPayload? contact = conn.findContactByName("Alice");

// By public key prefix
byte[] prefix = new byte[] { 0x01, 0x02 };
ContactPayload? contact = conn.findContactByPublicKeyPrefix(prefix);
```

### Add/Update Contact
```csharp
conn.sendCommandAddUpdateContact(
    publicKey: publicKey,           // 32 bytes
    type: Constants.AdvType.Chat,   // Contact type
    flags: 0,
    outPathLen: 0,
    outPath: new byte[64],          // Routing path
    advName: "Contact Name",
    lastAdvert: 0,
    advLat: 0,
    advLon: 0
);

conn.OkResponse += (s, e) => Console.WriteLine("Added!");
```

### Remove Contact
```csharp
conn.sendCommandRemoveContact(publicKey);
```

### Export Contact
```csharp
conn.ExportContactResponse += (s, data) =>
{
    // data = raw advertisement packet
};
conn.sendCommandExportContact(publicKey);
```

### Import Contact
```csharp
conn.sendCommandImportContact(advertPacketBytes);
```

### Share Contact
```csharp
conn.sendCommandShareContact(publicKey);
```

---

## Messaging Operations

### Send Message to Contact
```csharp
SentPayload? result = conn.sendTextMessage(
    contactPublicKey: contact.PublicKey,
    text: "Hello!",
    txtType: Constants.TxtType.Plain
);

if (result != null)
{
    Console.WriteLine($"Queue position: {result.QueuePos}");
    Console.WriteLine($"Est timeout: {result.EstTimeout}ms");
}
```

### Send Channel Message
```csharp
conn.sendChannelTextMessage(
    channelIdx: 0,
    text: "Hello channel!"
);
```

### Receive Contact Messages
```csharp
conn.ContactMsgRecv += (s, msg) =>
{
    Console.WriteLine($"From: {BitConverter.ToString(msg.PubKeyPrefix)}");
    Console.WriteLine($"Text: {msg.Text}");
    Console.WriteLine($"Time: {msg.SenderTimestamp}");
};
```

### Receive Channel Messages
```csharp
conn.ChannelMsgRecv += (s, msg) =>
{
    Console.WriteLine($"Channel {msg.ChannelIdx}: {msg.Text}");
};
```

### Sync Next Message
```csharp
MsgPayload? msg = conn.syncNextMessage();
if (msg != null)
{
    Console.WriteLine($"{msg.Text} (IsContact: {msg.IsContact})");
}
```

### Get All Waiting Messages
```csharp
Queue<MsgPayload> messages = conn.getWaitingMessages();
foreach (var msg in messages)
{
    Console.WriteLine(msg.Text);
}
```

---

## Channel Operations

### Get Channel Info
```csharp
conn.ChannelInfoResponse += (s, channel) =>
{
    Console.WriteLine($"Channel {channel.Idx}: {channel.Name}");
};
conn.sendCommandGetChannel(0);
```

### Set Channel
```csharp
byte[] secret = new byte[16]; // Encryption key
conn.sendCommandSetChannel(
    channelIdx: 0,
    name: "My Channel",
    secret: secret
);
```

---

## Path Operations

### Reset Path
```csharp
conn.sendCommandResetPath(contactPublicKey);
```

### Trace Path
```csharp
conn.TraceDataPush += (s, trace) =>
{
    Console.WriteLine($"Tag: {trace.Tag}");
    Console.WriteLine($"Hops: {trace.PathLen}");
    Console.WriteLine($"RSSI: {trace.LastRssi}dBm");
};

byte[] path = new byte[] { 0x01, 0x02 }; // Node IDs
conn.sendCommandSendTracePath(
    tag: 12345,
    auth: 0,
    path: path
);
```

---

## Time Operations

### Get Device Time
```csharp
uint? time = conn.getDeviceTime();
if (time.HasValue)
{
    var dt = DateTimeOffset.FromUnixTimeSeconds(time.Value);
    Console.WriteLine($"Device time: {dt}");
}
```

### Set Device Time
```csharp
uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
conn.setDeviceTime(now);
```

---

## Advanced Operations

### Send Advertisement
```csharp
// Flood (multi-hop)
conn.sendFloodAdvert();

// Zero-hop (direct neighbors only)
conn.sendZeroHopAdvert();
```

### Send Raw Data
```csharp
conn.RawDataPush += (s, data) =>
{
    Console.WriteLine($"Received: {BitConverter.ToString(data.Raw)}");
    Console.WriteLine($"RSSI: {data.LastRssi}dBm");
};

byte[] path = new byte[] { 0x01 };
byte[] rawData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
conn.sendCommandSendRawData(path, rawData);
```

### Login to Repeater
```csharp
conn.LoginSuccessPush += (s, login) =>
{
    Console.WriteLine($"Logged in! Hop limit: {login.HopLimit}");
};

conn.sendCommandSendLogin(
    publicKey: repeaterPublicKey,
    password: "mypassword"
);
```

### Request Status
```csharp
conn.StatusResponsePush += (s, status) =>
{
    // Parse status data
};
conn.sendCommandSendStatusReq(nodePublicKey);
```

### Request Telemetry
```csharp
conn.TelemetryResponsePush += (s, telemetry) =>
{
    // Parse telemetry data
};
conn.sendCommandSendTelemetryReq(nodePublicKey);
```

### Export Private Key
```csharp
conn.PrivateKeyResponse += (s, key) =>
{
    Console.WriteLine($"Key: {BitConverter.ToString(key)}");
};
conn.sendCommandExportPrivateKey();
```

### Import Private Key
```csharp
byte[] privateKey = new byte[64];
conn.sendCommandImportPrivateKey(privateKey);
```

### Set Auto-Add Mode
```csharp
conn.sendCommandSetOtherParams(
    manualAddContacts: 0  // 0 = auto, 1 = manual
);
```

---

## Events Reference

### Push Events (Asynchronous)

| Event | Payload Type | Description |
|-------|-------------|-------------|
| `Connected` | `EventArgs` | Connection established |
| `FrameReceived` | `byte[]` | Raw frame received |
| `AdvertPush` | `byte[]` | Advertisement (auto-add) |
| `NewAdvertPush` | `NewAdvertPushPayload` | New advert (manual-add) |
| `PathUpdatedPush` | `byte[]` | Path updated |
| `SendConfirmedPush` | `SendConfirmedPushPayload` | Send confirmed |
| `MsgWaitingPush` | `EventArgs` | Messages waiting |
| `RawDataPush` | `RawDataPushPayload` | Raw data received |
| `LoginSuccessPush` | `LoginSuccessPushPayload` | Login successful |
| `StatusResponsePush` | `StatusResponsePushPayload` | Status response |
| `LogRxDataPush` | `LogRxDataPushPayload` | Logged RX data |
| `TelemetryResponsePush` | `TelemetryResponsePushPayload` | Telemetry data |
| `TraceDataPush` | `TraceDataPushPayload` | Trace results |

### Response Events

| Event | Payload Type | Description |
|-------|-------------|-------------|
| `OkResponse` | `EventArgs` | Command succeeded |
| `ErrResponse` | `EventArgs` | Command failed |
| `SelfInfoResponse` | `SelfInfoPayload` | Device info |
| `ContactsStartResponse` | `uint` | Contacts sync start (count) |
| `ContactResponse` | `ContactPayload` | Contact data |
| `EndOfContactsResponse` | `uint` | Contacts sync end |
| `SentResponse` | `SentPayload` | Message queued |
| `CurrentTimeResponse` | `uint` | Device time (epoch) |
| `NoMoreMessagesResponse` | `EventArgs` | No messages left |
| `ContactMsgRecv` | `ContactMsgPayload` | Contact message |
| `ChannelMsgRecv` | `ChannelMsgPayload` | Channel message |
| `ExportContactResponse` | `byte[]` | Contact export data |
| `BatteryVoltageResponse` | `ushort` | Battery mV |
| `DeviceInfoResponse` | `DeviceInfoPayload` | Device info |
| `PrivateKeyResponse` | `byte[]` | Private key (64 bytes) |
| `DisabledResponse` | `EventArgs` | Feature disabled |
| `ChannelInfoResponse` | `ChannelInfoPayload` | Channel info |

---

## Payload Types

### ContactPayload
```csharp
record ContactPayload(
    byte[] PublicKey,      // 32 bytes
    byte Type,             // AdvType
    byte Flags,
    sbyte OutPathLen,      // Path length
    byte[] OutPath,        // 64 bytes max
    string AdvName,        // Display name
    uint LastAdvert,       // Timestamp
    uint AdvLat,           // Latitude (microdegrees)
    uint AdvLon,           // Longitude (microdegrees)
    uint LastMod           // Last modified
);
```

### SelfInfoPayload
```csharp
record SelfInfoPayload(
    byte Ver,              // Protocol version
    byte AdvType,
    byte AdvFlags,
    byte[] PublicKey,      // 32 bytes
    int AdvLat,            // Latitude
    int AdvLon,            // Longitude
    byte[] Reserved,       // 3 bytes
    byte ManualAddContacts,
    uint RadioFreq,        // Frequency in Hz
    uint RadioBw,          // Bandwidth in Hz
    byte RadioSf,          // Spreading factor
    byte RadioCr,          // Coding rate
    string AdvName         // Device name
);
```

### MsgPayload
```csharp
record MsgPayload(
    string Source,         // Sender identifier
    byte PathLen,          // Routing path length
    byte TxtType,          // Message type
    uint SenderTimestamp,  // Unix timestamp
    string Text,           // Message text
    bool IsContact         // From contact vs channel
);
```

### SentPayload
```csharp
record SentPayload(
    sbyte QueuePos,        // Position in TX queue
    uint SenderTimestamp,  // Message timestamp
    uint EstTimeout        // Estimated timeout ms
);
```

---

## Constants Reference

### BLE UUIDs
```csharp
Constants.BleToUuid(Constants.Ble.ServiceUuid)
// "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"

Constants.BleToUuid(Constants.Ble.CharacteristicUuidRx)
// "6E400002-B5A3-F393-E0A9-E50E24DCCA9E"

Constants.BleToUuid(Constants.Ble.CharacteristicUuidTx)
// "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"
```

### Enumerations

#### AdvType
- `None` = 0
- `Chat` = 1
- `Repeater` = 2
- `Room` = 3

#### TxtType
- `Plain` = 0
- `CliData` = 1
- `SignedPlain` = 2

#### SelfAdvertTypes
- `ZeroHop` = 0 (neighbors only)
- `Flood` = 1 (multi-hop)

#### ErrorCodes
- `UnsupportedCmd` = 1
- `NotFound` = 2
- `TableFull` = 3
- `BadState` = 4
- `FileIoError` = 5
- `IllegalArg` = 6

---

## Common Patterns

### Request-Response Pattern
```csharp
bool success = false;

void OnOk(object? s, EventArgs e)
{
    conn.OkResponse -= OnOk;
    conn.ErrResponse -= OnErr;
    success = true;
}

void OnErr(object? s, EventArgs e)
{
    conn.OkResponse -= OnOk;
    conn.ErrResponse -= OnErr;
    success = false;
}

conn.OkResponse += OnOk;
conn.ErrResponse += OnErr;
conn.sendCommandSomeOperation();
```

### Wait for Response Pattern
```csharp
var waitHandle = new AutoResetEvent(false);
ContactPayload? result = null;

conn.ContactResponse += (s, contact) =>
{
    result = contact;
    waitHandle.Set();
};

conn.sendCommandGetContacts();
waitHandle.WaitOne(TimeSpan.FromSeconds(5));
```

### Continuous Monitoring Pattern
```csharp
conn.ContactMsgRecv += (s, msg) =>
{
    Task.Run(() => ProcessMessage(msg));
};
// Messages arrive and are processed in background
```

---

**Version**: 1.0  
**Last Updated**: 2024
