# Meshcore.NET - C# Wrapper Library

A comprehensive C# wrapper library for the Meshcore mesh networking protocol, enabling developers to build applications that communicate with Meshcore-compatible devices over Bluetooth Low Energy (BLE) or serial connections.

## 🌟 Overview

Meshcore.NET provides a high-level, type-safe API for interacting with Meshcore devices, which are part of a decentralized mesh network for off-grid communication. This library handles the low-level protocol details, allowing you to focus on building your application logic.

### Key Features

- ✅ **Type-safe API** - Strongly typed C# classes and enums
- ✅ **Event-driven architecture** - Subscribe to device events and responses
- ✅ **Message handling** - Send and receive text messages through the mesh network
- ✅ **Contact management** - Add, update, remove, and sync contacts
- ✅ **Device configuration** - Configure radio parameters, channels, and settings
- ✅ **Path management** - Manage routing paths through the mesh network
- ✅ **Time synchronization** - Sync device time with system time
- ✅ **BLE Support** - Connect to devices via Bluetooth Low Energy
- ✅ **.NET 9.0** - Built on the latest .NET platform

## 📦 Installation

### NuGet Package (Coming Soon)

```bash
dotnet add package Meshcore.NET
```

### Manual Installation

1. Clone this repository:
```bash
git clone https://github.com/helmutsreinis/meshcore.net.git
```

2. Add the project reference to your solution:
```bash
dotnet add reference path/to/meshcore-lib/meshcore-lib.csproj
```

### Prerequisites

- .NET 9.0 SDK or higher
- A Meshcore-compatible device (e.g., Meshtastic, LoRa mesh device)
- Bluetooth Low Energy support (for wireless connections) or Serial port access

## 🚀 Quick Start

### 1. Create a Custom Connection Implementation

Since `Connection` is an abstract class, you need to implement the connection-specific methods:

```csharp
using meshcore_lib.connection;

public class MyMeshcoreConnection : Connection
{
    private readonly IBluetoothDevice _device; // Your BLE implementation
    
    public MyMeshcoreConnection(IBluetoothDevice device)
    {
        _device = device;
    }
    
    protected override void Close()
    {
        // Close your BLE/Serial connection
        _device.Disconnect();
    }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        // Send data to your device via BLE or Serial
        _device.WriteCharacteristic(Constants.BleToUuid(Constants.Ble.CharacteristicUuidRx), data);
    }
    
    public void OnDataReceived(byte[] data)
    {
        // Called when data is received from the device
        onFrameReceived(data);
    }
}
```

### 2. Initialize and Connect

```csharp
using meshcore_lib;
using meshcore_lib.connection;

// Create your connection instance
var connection = new MyMeshcoreConnection(yourBluetoothDevice);

// Subscribe to connection event
connection.Connected += (sender, args) =>
{
    Console.WriteLine("Connected to Meshcore device!");
};

// Subscribe to events you're interested in
connection.ContactResponse += (sender, contact) =>
{
    Console.WriteLine($"Received contact: {contact.AdvName}");
};

// Trigger connection
connection.onConnected();
```

### 3. Send a Text Message

```csharp
// Get device information first
var selfInfo = connection.getSelfInfo();
Console.WriteLine($"Device: {selfInfo.AdvName}");

// Get contacts
var contacts = connection.getContacts();
Console.WriteLine($"Found {contacts.Count} contacts");

// Send a message to a contact
if (contacts.Count > 0)
{
    var contact = contacts[0];
    var result = connection.sendTextMessage(
        contact.PublicKey,
        "Hello from Meshcore.NET!",
        Constants.TxtType.Plain
    );
    
    if (result != null)
    {
        Console.WriteLine($"Message sent! Queue position: {result.QueuePos}");
    }
}
```

## 📚 Core Functionalities

### 1. Device Information and Configuration

#### Get Device Information
```csharp
// Get comprehensive device information
var selfInfo = connection.getSelfInfo();

Console.WriteLine($"Device Name: {selfInfo.AdvName}");
Console.WriteLine($"Public Key: {BitConverter.ToString(selfInfo.PublicKey)}");
Console.WriteLine($"Location: Lat {selfInfo.AdvLat}, Lon {selfInfo.AdvLon}");
Console.WriteLine($"Radio Frequency: {selfInfo.RadioFreq} Hz");
Console.WriteLine($"Bandwidth: {selfInfo.RadioBw}");
Console.WriteLine($"Spreading Factor: {selfInfo.RadioSf}");
```

#### Set Device Name
```csharp
// Set the advertised name for your device
connection.setAdvertName("My Mesh Node");
```

#### Set Device Location
```csharp
// Set latitude and longitude (in microdegrees)
int latitude = 37774900;   // 37.7749°N (San Francisco)
int longitude = -122419400; // -122.4194°W
connection.setAdvertLatLong(latitude, longitude);
```

#### Configure Radio Parameters
```csharp
// Configure LoRa radio settings
uint frequency = 915000000;  // 915 MHz
uint bandwidth = 125000;     // 125 kHz
byte spreadingFactor = 7;    // SF7
byte codingRate = 5;         // 4/5 coding rate

connection.setRadioParams(frequency, bandwidth, spreadingFactor, codingRate);
```

#### Set Transmission Power
```csharp
// Set TX power (typically 2-20 for LoRa devices)
byte txPower = 20; // Maximum power
connection.setTxPower(txPower);
```

### 2. Contact Management

#### Get All Contacts
```csharp
// Retrieve all stored contacts from the device
var contacts = connection.getContacts();

foreach (var contact in contacts)
{
    Console.WriteLine($"Name: {contact.AdvName}");
    Console.WriteLine($"Type: {contact.Type}");
    Console.WriteLine($"Last Advert: {contact.LastAdvert}");
    Console.WriteLine($"Path Length: {contact.OutPathLen}");
    Console.WriteLine();
}
```

#### Find Contact by Name
```csharp
// Search for a specific contact
var contact = connection.findContactByName("Alice");
if (contact != null)
{
    Console.WriteLine($"Found: {contact.AdvName}");
    Console.WriteLine($"Public Key: {BitConverter.ToString(contact.PublicKey)}");
}
```

#### Find Contact by Public Key Prefix
```csharp
// Find contact using partial public key match
byte[] pubKeyPrefix = new byte[] { 0x01, 0x02, 0x03 };
var contact = connection.findContactByPublicKeyPrefix(pubKeyPrefix);
```

#### Add or Update Contact
```csharp
// Add a new contact or update existing one
byte[] publicKey = new byte[32]; // 32-byte public key
byte[] outPath = new byte[64];   // Routing path (up to 64 hops)

connection.sendCommandAddUpdateContact(
    publicKey: publicKey,
    type: Constants.AdvType.Chat,
    flags: 0,
    outPathLen: 0,
    outPath: outPath,
    advName: "New Contact",
    lastAdvert: 0,
    advLat: 0,
    advLon: 0
);

// Listen for response
connection.OkResponse += (sender, args) =>
{
    Console.WriteLine("Contact added successfully!");
};
```

### 3. Messaging

#### Send Direct Message
```csharp
// Send a text message to a contact
var contact = connection.findContactByName("Bob");
if (contact != null)
{
    var result = connection.sendTextMessage(
        contactPublicKey: contact.PublicKey,
        text: "Hello, Bob!",
        txtType: Constants.TxtType.Plain
    );
    
    if (result != null)
    {
        Console.WriteLine($"Message queued at position: {result.QueuePos}");
        Console.WriteLine($"Estimated timeout: {result.EstTimeout}ms");
    }
}
```

#### Send Channel Message
```csharp
// Send a message to a channel
byte channelIdx = 0; // Channel index
connection.sendChannelTextMessage(channelIdx, "Hello channel!");
```

#### Receive Messages
```csharp
// Subscribe to incoming contact messages
connection.ContactMsgRecv += (sender, msg) =>
{
    Console.WriteLine($"From: {BitConverter.ToString(msg.PubKeyPrefix)}");
    Console.WriteLine($"Message: {msg.Text}");
    Console.WriteLine($"Timestamp: {msg.SenderTimestamp}");
};

// Subscribe to incoming channel messages
connection.ChannelMsgRecv += (sender, msg) =>
{
    Console.WriteLine($"Channel {msg.ChannelIdx}: {msg.Text}");
};
```

#### Sync Messages
```csharp
// Retrieve next waiting message
var message = connection.syncNextMessage();
if (message != null)
{
    Console.WriteLine($"Message: {message.Text}");
    Console.WriteLine($"From contact: {message.IsContact}");
}

// Get all waiting messages
var messages = connection.getWaitingMessages();
Console.WriteLine($"Retrieved {messages.Count} messages");
foreach (var msg in messages)
{
    Console.WriteLine($"- {msg.Text}");
}
```

### 4. Channel Management

#### Get Channel Information
```csharp
// Listen for channel info response
connection.ChannelInfoResponse += (sender, channelInfo) =>
{
    Console.WriteLine($"Channel {channelInfo.Idx}: {channelInfo.Name}");
    Console.WriteLine($"Secret: {BitConverter.ToString(channelInfo.Secret)}");
};

// Request channel info
connection.sendCommandGetChannel(0); // Get channel at index 0
```

#### Set Channel Configuration
```csharp
// Configure a channel
byte channelIdx = 0;
string channelName = "Team Channel";
byte[] channelSecret = new byte[16]; // 16-byte encryption key
// ... fill in secret ...

connection.sendCommandSetChannel(channelIdx, channelName, channelSecret);

// Listen for confirmation
connection.OkResponse += (sender, args) =>
{
    Console.WriteLine("Channel configured successfully!");
};
```

### 5. Time Synchronization

#### Get Device Time
```csharp
// Retrieve current device time (Unix epoch seconds)
var deviceTime = connection.getDeviceTime();
if (deviceTime.HasValue)
{
    var dateTime = DateTimeOffset.FromUnixTimeSeconds(deviceTime.Value);
    Console.WriteLine($"Device time: {dateTime}");
}
```

#### Set Device Time
```csharp
// Sync device time to current system time
uint currentTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
connection.setDeviceTime(currentTime);
```

### 6. Path Management

#### Reset Contact Path
```csharp
// Reset the routing path for a contact
var contact = connection.findContactByName("Alice");
if (contact != null)
{
    connection.sendCommandResetPath(contact.PublicKey);
}
```

#### Send Trace Path
```csharp
// Trace a path through the network
byte[] path = new byte[] { 0x01, 0x02, 0x03 }; // Node IDs in path
uint tag = 12345;  // Unique identifier for this trace
uint auth = 0;     // Authentication value

connection.sendCommandSendTracePath(tag, auth, path);

// Listen for trace results
connection.TraceDataPush += (sender, traceData) =>
{
    Console.WriteLine($"Trace tag: {traceData.Tag}");
    Console.WriteLine($"Path length: {traceData.PathLen}");
    Console.WriteLine($"RSSI: {traceData.LastRssi} dBm");
};
```

### 7. Advertisement Broadcasting

#### Send Flood Advertisement
```csharp
// Broadcast your presence across the mesh network
connection.sendFloodAdvert();
```

#### Send Zero-Hop Advertisement
```csharp
// Advertise only to direct neighbors
connection.sendZeroHopAdvert();
```

### 8. Advanced Features

#### Export Contact
```csharp
// Export a contact's information as raw bytes
var contact = connection.findContactByName("Alice");

connection.ExportContactResponse += (sender, data) =>
{
    Console.WriteLine($"Contact data: {BitConverter.ToString(data)}");
    // Save or share this data
};

connection.sendCommandExportContact(contact.PublicKey);
```

#### Import Contact
```csharp
// Import contact from raw advertisement packet bytes
byte[] advertPacketBytes = /* ... received from somewhere ... */;
connection.sendCommandImportContact(advertPacketBytes);
```

#### Share Contact
```csharp
// Share a contact with other nodes
var contact = connection.findContactByName("Alice");
connection.sendCommandShareContact(contact.PublicKey);
```

#### Remove Contact
```csharp
// Remove a contact from the device
var contact = connection.findContactByName("OldContact");
connection.sendCommandRemoveContact(contact.PublicKey);
```

#### Reboot Device
```csharp
// Reboot the Meshcore device
connection.sendCommandReboot();
```

#### Get Battery Voltage
```csharp
// Get current battery voltage
connection.BatteryVoltageResponse += (sender, milliVolts) =>
{
    double volts = milliVolts / 1000.0;
    Console.WriteLine($"Battery: {volts:F2}V");
};

connection.sendCommandGetBatteryVoltage();
```

#### Raw Data Communication
```csharp
// Send raw data packets through the mesh
byte[] path = new byte[] { 0x01 }; // Destination node
byte[] rawData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

connection.sendCommandSendRawData(path, rawData);

// Receive raw data
connection.RawDataPush += (sender, data) =>
{
    Console.WriteLine($"Raw data received: {BitConverter.ToString(data.Raw)}");
    Console.WriteLine($"RSSI: {data.LastRssi} dBm");
    Console.WriteLine($"SNR: {data.LastSnr} dB");
};
```

#### Login to Repeater/Room
```csharp
// Login to a repeater or room server
var repeater = connection.findContactByName("Repeater1");
string password = "mypassword";

connection.LoginSuccessPush += (sender, loginData) =>
{
    Console.WriteLine($"Login successful! Hop limit: {loginData.HopLimit}");
};

connection.sendCommandSendLogin(repeater.PublicKey, password);
```

#### Request Status from Repeater
```csharp
// Request status information from a repeater
var repeater = connection.findContactByName("Repeater1");

connection.StatusResponsePush += (sender, status) =>
{
    Console.WriteLine($"Status data: {BitConverter.ToString(status.StatusData)}");
    // Parse status data according to your protocol
};

connection.sendCommandSendStatusReq(repeater.PublicKey);
```

#### Request Telemetry
```csharp
// Request telemetry data from a node
var node = connection.findContactByName("SensorNode");

connection.TelemetryResponsePush += (sender, telemetry) =>
{
    Console.WriteLine($"Telemetry data: {BitConverter.ToString(telemetry.TelemetryData)}");
    // Parse telemetry according to your sensor format
};

connection.sendCommandSendTelemetryReq(node.PublicKey);
```

## 🔧 Integration Guide

### Step 1: Understand the Architecture

Meshcore.NET follows an event-driven architecture:
- **Commands** are sent to the device using `sendCommand*` methods
- **Responses** are received via event handlers
- **Push notifications** arrive asynchronously from the device

### Step 2: Implement Connection Layer

You need to implement the abstract `Connection` class for your specific transport:

```csharp
public abstract class Connection
{
    // Implement these methods:
    protected abstract void Close();
    protected abstract void SendToRadioFrame(byte[] data);
    
    // Call this when connection is established:
    public void onConnected();
    
    // Call this when data arrives from device:
    public void onFrameReceived(byte[] frame);
}
```

### Step 3: Handle BLE Communication

For Bluetooth Low Energy connections:

```csharp
// Use the UUID constants for BLE services
string serviceUuid = Constants.BleToUuid(Constants.Ble.ServiceUuid);
string rxCharUuid = Constants.BleToUuid(Constants.Ble.CharacteristicUuidRx);
string txCharUuid = Constants.BleToUuid(Constants.Ble.CharacteristicUuidTx);

// Write to RX characteristic to send to device
// Read from TX characteristic to receive from device
```

### Step 4: Handle Serial Communication

For serial connections, you can use the `SerialFrameTypes` to frame data:

```csharp
byte[] CreateSerialFrame(byte[] data)
{
    var frame = new byte[data.Length + 2];
    frame[0] = (byte)Constants.SerialFrameTypes.Outgoing; // '>'
    Array.Copy(data, 0, frame, 1, data.Length);
    frame[frame.Length - 1] = (byte)Constants.SerialFrameTypes.Incoming; // '<'
    return frame;
}
```

### Step 5: Error Handling

Always subscribe to error responses:

```csharp
connection.ErrResponse += (sender, args) =>
{
    Console.WriteLine("Command failed!");
    // Handle error appropriately
};

connection.OkResponse += (sender, args) =>
{
    Console.WriteLine("Command succeeded!");
};
```

### Step 6: Thread Safety

The library uses locks internally for some operations. When using events:
- Event handlers are called on the same thread that calls `onFrameReceived`
- Long-running operations in event handlers should be offloaded to background threads
- Be careful with UI updates - marshal to UI thread as needed

```csharp
connection.ContactResponse += async (sender, contact) =>
{
    // Offload heavy work
    await Task.Run(() =>
    {
        ProcessContact(contact);
    });
};
```

## 📖 API Reference

### Constants Class

Contains all protocol constants, enums, and helper methods:

- **BLE UUIDs**: Service and characteristic UUIDs for Bluetooth connectivity
- **Command Codes**: All supported device commands
- **Response Codes**: Response types from the device
- **Push Codes**: Asynchronous notifications from device
- **Error Codes**: Error types that can be returned
- **Enums**: AdvType, SelfAdvertTypes, TxtType, etc.

### Connection Class (Abstract)

Main class for device interaction. Key methods:

#### Command Methods
- `sendCommandAppStart()` - Initialize app connection
- `sendCommandSendTxtMsg()` - Send text message
- `sendCommandSendChannelTxtMsg()` - Send channel message
- `sendCommandGetContacts()` - Request contacts list
- `sendCommandGetDeviceTime()` / `sendCommandSetDeviceTime()` - Time management
- `sendCommandSendSelfAdvert()` - Broadcast advertisement
- `sendCommandSetAdvertName()` - Set device name
- `sendCommandAddUpdateContact()` - Manage contacts
- `sendCommandSyncNextMessage()` - Retrieve messages
- `sendCommandSetRadioParams()` - Configure radio
- `sendCommandSetTxPower()` - Set transmission power
- `sendCommandResetPath()` - Reset routing path
- `sendCommandSetAdvertLatLon()` - Set location
- `sendCommandRemoveContact()` - Remove contact
- `sendCommandShareContact()` - Share contact
- `sendCommandExportContact()` - Export contact data
- `sendCommandImportContact()` - Import contact
- `sendCommandReboot()` - Reboot device
- `sendCommandGetBatteryVoltage()` - Get battery info
- `sendCommandDeviceQuery()` - Query device info
- `sendCommandExportPrivateKey()` / `sendCommandImportPrivateKey()` - Key management
- `sendCommandSendRawData()` - Send raw data
- `sendCommandSendLogin()` - Login to repeater/room
- `sendCommandSendStatusReq()` - Request status
- `sendCommandGetChannel()` / `sendCommandSetChannel()` - Channel management
- `sendCommandSendTracePath()` - Trace network path
- `sendCommandSetOtherParams()` - Set other parameters
- `sendCommandSendTelemetryReq()` - Request telemetry

#### Helper Methods
- `getSelfInfo()` - Get device info (blocking)
- `sendFloodAdvert()` - Flood advertisement (blocking)
- `sendZeroHopAdvert()` - Zero-hop advertisement (blocking)
- `setAdvertName()` - Set name (blocking)
- `setAdvertLatLong()` - Set location (blocking)
- `setTxPower()` - Set TX power (blocking)
- `setRadioParams()` - Set radio params (blocking)
- `getContacts()` - Get all contacts (blocking)
- `findContactByName()` - Find contact by name
- `findContactByPublicKeyPrefix()` - Find contact by key
- `sendTextMessage()` - Send message (blocking)
- `sendChannelTextMessage()` - Send to channel (blocking)
- `syncNextMessage()` - Get next message (blocking)
- `getWaitingMessages()` - Get all messages (blocking)
- `getDeviceTime()` - Get time (blocking)
- `setDeviceTime()` - Set time (blocking)

#### Events
- `Connected` - Device connected
- `FrameReceived` - Raw frame received
- `AdvertPush` - Advertisement received
- `PathUpdatedPush` - Path updated
- `SendConfirmedPush` - Send confirmed
- `MsgWaitingPush` - Messages waiting
- `RawDataPush` - Raw data received
- `LoginSuccessPush` - Login successful
- `StatusResponsePush` - Status response
- `LogRxDataPush` - Logged RX data
- `TelemetryResponsePush` - Telemetry data
- `TraceDataPush` - Trace results
- `NewAdvertPush` - New advertisement
- `OkResponse` - Command succeeded
- `ErrResponse` - Command failed
- `ContactsStartResponse` - Contacts sync started
- `ContactResponse` - Contact received
- `EndOfContactsResponse` - Contacts sync ended
- `SentResponse` - Message sent
- `ExportContactResponse` - Contact exported
- `BatteryVoltageResponse` - Battery voltage
- `DeviceInfoResponse` - Device info
- `PrivateKeyResponse` - Private key
- `DisabledResponse` - Feature disabled
- `ChannelInfoResponse` - Channel info
- `SelfInfoResponse` - Self info
- `CurrentTimeResponse` - Current time
- `NoMoreMessagesResponse` - No more messages
- `ContactMsgRecv` - Contact message received
- `ChannelMsgRecv` - Channel message received

### Utility Classes

#### BufferWriter
Helper for writing binary data:
- `WriteByte()` - Write single byte
- `WriteBytes()` - Write byte array
- `WriteString()` - Write UTF-8 string
- `WriteUInt16LE()` / `WriteUInt32LE()` - Write integers (little-endian)
- `WriteInt32LE()` - Write signed integer
- `WriteCString()` - Write null-terminated string

#### BufferReader
Helper for reading binary data:
- `ReadByte()` - Read single byte
- `ReadBytes()` - Read byte array
- `ReadRemainingBytes()` - Read all remaining
- `ReadString()` - Read UTF-8 string
- `ReadUInt16LE()` / `ReadUInt32LE()` - Read unsigned integers
- `ReadInt16LE()` / `ReadInt32LE()` - Read signed integers
- `ReadCString()` - Read null-terminated string

### Payload Classes

Data transfer objects for various responses:
- `ContactPayload` - Contact information
- `MsgPayload` - General message
- `ContactMsgPayload` - Contact message
- `ChannelMsgPayload` - Channel message
- `SelfInfoPayload` - Device self-information
- `SentPayload` - Message sent confirmation
- `DeviceInfoPayload` - Device information
- `ChannelInfoPayload` - Channel configuration
- `SendConfirmedPushPayload` - Send confirmation
- `RawDataPushPayload` - Raw data packet
- `LoginSuccessPushPayload` - Login confirmation
- `StatusResponsePushPayload` - Status data
- `LogRxDataPushPayload` - Logged RX data
- `TelemetryResponsePushPayload` - Telemetry data
- `TraceDataPushPayload` - Path trace results
- `NewAdvertPushPayload` - New advertisement

## 🎯 Example Projects

### Example 1: Simple Message Monitor

```csharp
public class MessageMonitor
{
    private readonly Connection _connection;
    
    public MessageMonitor(Connection connection)
    {
        _connection = connection;
        SetupEventHandlers();
    }
    
    private void SetupEventHandlers()
    {
        _connection.Connected += OnConnected;
        _connection.ContactMsgRecv += OnContactMessage;
        _connection.ChannelMsgRecv += OnChannelMessage;
    }
    
    private void OnConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("Connected! Monitoring messages...");
        
        // Get device info
        var info = _connection.getSelfInfo();
        Console.WriteLine($"Device: {info.AdvName}");
        
        // Check for waiting messages
        var messages = _connection.getWaitingMessages();
        Console.WriteLine($"Found {messages.Count} waiting messages");
    }
    
    private void OnContactMessage(object? sender, ContactMsgPayload msg)
    {
        Console.WriteLine($"[{DateTime.Now}] Contact message:");
        Console.WriteLine($"  From: {BitConverter.ToString(msg.PubKeyPrefix)}");
        Console.WriteLine($"  Text: {msg.Text}");
    }
    
    private void OnChannelMessage(object? sender, ChannelMsgPayload msg)
    {
        Console.WriteLine($"[{DateTime.Now}] Channel {msg.ChannelIdx}:");
        Console.WriteLine($"  Text: {msg.Text}");
    }
}
```

### Example 2: Automatic Contact Sync

```csharp
public class ContactSyncService
{
    private readonly Connection _connection;
    private readonly List<ContactPayload> _cachedContacts = new();
    
    public ContactSyncService(Connection connection)
    {
        _connection = connection;
        _connection.Connected += OnConnected;
        _connection.NewAdvertPush += OnNewAdvertisement;
    }
    
    private void OnConnected(object? sender, EventArgs e)
    {
        RefreshContacts();
    }
    
    private void OnNewAdvertisement(object? sender, NewAdvertPushPayload advert)
    {
        Console.WriteLine($"New device discovered: {advert.Contact.AdvName}");
        RefreshContacts();
    }
    
    public void RefreshContacts()
    {
        _cachedContacts.Clear();
        var contacts = _connection.getContacts();
        _cachedContacts.AddRange(contacts);
        
        Console.WriteLine($"Synced {contacts.Count} contacts:");
        foreach (var contact in contacts)
        {
            Console.WriteLine($"  - {contact.AdvName} ({contact.Type})");
        }
    }
    
    public ContactPayload? FindContact(string name)
    {
        return _cachedContacts.FirstOrDefault(c => 
            c.AdvName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
```

### Example 3: Configuration Manager

```csharp
public class DeviceConfigurator
{
    private readonly Connection _connection;
    
    public DeviceConfigurator(Connection connection)
    {
        _connection = connection;
    }
    
    public void ConfigureForLongRange()
    {
        // Configure for maximum range (slower data rate)
        _connection.setRadioParams(
            radioFreq: 915000000,    // 915 MHz
            radioBw: 125000,         // 125 kHz bandwidth
            radioSf: 12,             // SF12 - maximum range
            radioCr: 8               // 4/8 coding rate
        );
        
        _connection.setTxPower(20);  // Maximum power
        Console.WriteLine("Configured for long range");
    }
    
    public void ConfigureForHighSpeed()
    {
        // Configure for faster data rate (shorter range)
        _connection.setRadioParams(
            radioFreq: 915000000,
            radioBw: 500000,         // 500 kHz bandwidth
            radioSf: 7,              // SF7 - minimum
            radioCr: 5               // 4/5 coding rate
        );
        
        Console.WriteLine("Configured for high speed");
    }
    
    public void SetLocation(double latitude, double longitude)
    {
        // Convert to microdegrees
        int lat = (int)(latitude * 1_000_000);
        int lon = (int)(longitude * 1_000_000);
        
        _connection.setAdvertLatLong(lat, lon);
        Console.WriteLine($"Location set to {latitude}, {longitude}");
    }
}
```

## 🔍 Troubleshooting

### Build Issues

**Problem**: Missing .NET 9.0 SDK
```
Solution: Install .NET 9.0 SDK from https://dotnet.microsoft.com/download
```

**Problem**: Reference errors
```
Solution: Ensure all NuGet packages are restored:
dotnet restore
```

### Runtime Issues

**Problem**: Events not firing
```csharp
// Ensure you're calling onFrameReceived when data arrives:
protected override void OnDataFromDevice(byte[] data)
{
    onFrameReceived(data);  // This triggers the event system
}
```

**Problem**: Timeout errors
```csharp
// Some methods have optional timeout parameters:
var selfInfo = connection.getSelfInfo(timeoutMilis: 10000); // 10 seconds
```

**Problem**: Null reference on events
```csharp
// Events are not automatically initialized. Check for null or use null-conditional:
connection.ContactResponse?.Invoke(this, contact);
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Based on the Meshcore mesh networking protocol
- Inspired by the JavaScript meshcore wrapper implementation
- Built with ❤️ for the mesh networking community

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/helmutsreinis/meshcore.net/issues)
- **Documentation**: This README and inline code documentation
- **Community**: Join the mesh networking community discussions

## 🗺️ Roadmap

- [ ] Complete implementation of async methods (currently JavaScript stubs)
- [ ] NuGet package publication
- [ ] More example projects
- [ ] Unit tests
- [ ] Performance benchmarks
- [ ] Additional BLE platform support
- [ ] Serial port connection implementation
- [ ] Enhanced error handling and diagnostics

---

**Version**: 1.0.0  
**Last Updated**: 2024  
**Platform**: .NET 9.0+
