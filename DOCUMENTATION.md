# Meshcore.NET Documentation Summary

## Overview
This document provides a comprehensive guide to the Meshcore.NET library functionality and integration approaches.

## Library Architecture

### Core Components

1. **Constants Class** (`meshcore_lib.Constants`)
   - Protocol version constants
   - BLE UUID definitions and converters
   - Command codes enumeration
   - Response codes enumeration
   - Push notification codes
   - Error codes
   - Type enumerations (AdvType, TxtType, etc.)

2. **Connection Class** (`meshcore_lib.connection.Connection`)
   - Abstract base class for device connections
   - Event-driven communication model
   - Command methods for device interaction
   - Helper methods with synchronous blocking operations
   - Thread-safe operations using internal locking

3. **Utility Classes**
   - `BufferWriter` - Binary data writing with protocol-specific formats
   - `BufferReader` - Binary data parsing from device frames

4. **Payload Classes** (`meshcore_lib.connection.payloads.*`)
   - Strongly-typed data transfer objects
   - Immutable record types for data integrity

## Integration Approaches

### Approach 1: Direct BLE Integration

**Use Case**: Native Bluetooth connectivity in desktop or mobile applications

**Implementation Steps**:
1. Create a class inheriting from `Connection`
2. Implement `Close()` to disconnect BLE
3. Implement `SendToRadioFrame()` to write to BLE characteristic
4. Subscribe to BLE notifications and call `onFrameReceived()` when data arrives
5. Call `onConnected()` when BLE connection is established

**Example**:
```csharp
public class BleConnection : Connection
{
    private readonly IBleDevice _device;
    
    public BleConnection(IBleDevice device)
    {
        _device = device;
        _device.CharacteristicChanged += OnDataReceived;
    }
    
    protected override void Close()
    {
        _device.Disconnect();
    }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        var rxUuid = Constants.BleToUuid(Constants.Ble.CharacteristicUuidRx);
        _device.WriteCharacteristic(rxUuid, data);
    }
    
    private void OnDataReceived(byte[] data)
    {
        onFrameReceived(data);
    }
}
```

### Approach 2: Serial Port Integration

**Use Case**: USB or UART serial connections to devices

**Implementation Steps**:
1. Create a class inheriting from `Connection`
2. Implement `Close()` to close serial port
3. Implement `SendToRadioFrame()` to write to serial port with framing
4. Read from serial port and parse frames using `SerialFrameTypes`
5. Call `onFrameReceived()` for each complete frame

**Example**:
```csharp
public class SerialConnection : Connection
{
    private readonly SerialPort _port;
    
    public SerialConnection(string portName)
    {
        _port = new SerialPort(portName, 115200);
        _port.DataReceived += OnDataReceived;
        _port.Open();
    }
    
    protected override void Close()
    {
        _port.Close();
    }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        // Add frame markers
        var frame = new byte[data.Length + 2];
        frame[0] = (byte)Constants.SerialFrameTypes.Outgoing;
        Array.Copy(data, 0, frame, 1, data.Length);
        frame[frame.Length - 1] = (byte)Constants.SerialFrameTypes.Incoming;
        
        _port.Write(frame, 0, frame.Length);
    }
    
    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // Parse frames and extract data between markers
        // Then call: onFrameReceived(frameData);
    }
}
```

### Approach 3: Network Bridge Integration

**Use Case**: Remote device access through a network bridge/gateway

**Implementation Steps**:
1. Create a class inheriting from `Connection`
2. Implement `Close()` to close network connection
3. Implement `SendToRadioFrame()` to send over TCP/WebSocket
4. Receive data from network and call `onFrameReceived()`

**Example**:
```csharp
public class NetworkConnection : Connection
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    
    public NetworkConnection(string host, int port)
    {
        _client = new TcpClient(host, port);
        _stream = _client.GetStream();
        StartReceiving();
    }
    
    protected override void Close()
    {
        _stream.Close();
        _client.Close();
    }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        // Send length prefix + data
        var lengthBytes = BitConverter.GetBytes(data.Length);
        _stream.Write(lengthBytes, 0, 4);
        _stream.Write(data, 0, data.Length);
    }
    
    private async void StartReceiving()
    {
        // Read frames from network stream
        // Call: onFrameReceived(frameData);
    }
}
```

## Functionality Categories

### 1. Device Management
- Query device information
- Set device name and location
- Configure radio parameters
- Reboot device
- Battery monitoring

### 2. Contact Management
- Get contacts list
- Add/update contacts
- Remove contacts
- Find contacts by name or key
- Export/import contacts
- Share contacts

### 3. Messaging
- Send direct messages
- Send channel messages
- Receive messages (contact and channel)
- Sync waiting messages
- Message confirmation tracking

### 4. Channel Operations
- Get channel configuration
- Set channel parameters
- List all channels
- Find channels by name or secret

### 5. Path Management
- Reset routing paths
- Trace network paths
- Update contact paths
- Monitor path updates

### 6. Network Operations
- Broadcast advertisements (flood/zero-hop)
- Raw data transmission
- Login to repeaters/rooms
- Request status from nodes
- Request telemetry data

### 7. Time Synchronization
- Get device time
- Set device time
- Sync with system time

### 8. Advanced Features
- Private key export/import
- Security logging
- Trace path diagnostics
- Custom parameter configuration

## Event Handling Patterns

### Pattern 1: Request-Response
```csharp
// Subscribe to response
connection.OkResponse += (sender, args) =>
{
    Console.WriteLine("Success!");
};

// Send command
connection.sendCommandSetAdvertName("NewName");
```

### Pattern 2: Synchronous Operations
```csharp
// Some operations have blocking helpers
var contacts = connection.getContacts(); // Blocks until complete
var message = connection.syncNextMessage(); // Blocks until message arrives
```

### Pattern 3: Push Notifications
```csharp
// Subscribe to push events
connection.NewAdvertPush += (sender, advert) =>
{
    Console.WriteLine($"New device: {advert.Contact.AdvName}");
};
// Events fire automatically when device sends notifications
```

### Pattern 4: Async Data Streams
```csharp
// Handle continuous data
connection.ContactMsgRecv += async (sender, msg) =>
{
    await ProcessMessageAsync(msg);
};
```

## Thread Safety Considerations

- The library uses internal locking for synchronous operations
- Event handlers execute on the thread that calls `onFrameReceived()`
- Long-running event handlers should offload work to background threads
- UI updates from event handlers should be marshaled to UI thread

## Error Handling

### Command Errors
```csharp
connection.OkResponse += (s, e) => Console.WriteLine("Success");
connection.ErrResponse += (s, e) => Console.WriteLine("Failed");
connection.sendCommandSomeOperation();
```

### Timeout Handling
```csharp
// Blocking operations support timeouts
try
{
    var info = connection.getSelfInfo(timeoutMilis: 5000);
}
catch (IOException ex)
{
    Console.WriteLine("Timeout or error: " + ex.Message);
}
```

### Connection Errors
```csharp
// Implement error handling in your connection class
protected override void SendToRadioFrame(byte[] data)
{
    try
    {
        _transport.Write(data);
    }
    catch (IOException ex)
    {
        // Handle connection loss
        Console.WriteLine("Connection lost: " + ex.Message);
    }
}
```

## Best Practices

1. **Initialize Connection Early**
   - Set up all event handlers before calling `onConnected()`
   - Subscribe to `Connected` event to know when ready

2. **Resource Cleanup**
   - Call `Close()` when done
   - Dispose of streams and connections properly

3. **Event Handler Management**
   - Unsubscribe from events when no longer needed
   - Avoid memory leaks with event subscriptions

4. **Error Resilience**
   - Always subscribe to `ErrResponse`
   - Handle timeouts appropriately
   - Implement reconnection logic

5. **Performance**
   - Don't block event handlers with heavy operations
   - Use async/await for I/O operations in handlers
   - Cache frequently accessed data (contacts, etc.)

6. **Security**
   - Protect private keys if exported
   - Validate input data from network
   - Use secure channels for sensitive operations

## Testing Integration

### Unit Testing Connection
```csharp
public class MockConnection : Connection
{
    private readonly Queue<byte[]> _responses = new();
    
    public void QueueResponse(byte[] response)
    {
        _responses.Enqueue(response);
    }
    
    protected override void Close() { }
    
    protected override void SendToRadioFrame(byte[] data)
    {
        // Verify command was sent correctly
        if (_responses.Count > 0)
        {
            var response = _responses.Dequeue();
            onFrameReceived(response);
        }
    }
}
```

### Testing Event Handlers
```csharp
[Test]
public void TestContactReceived()
{
    var connection = new MockConnection();
    ContactPayload? received = null;
    
    connection.ContactResponse += (s, contact) =>
    {
        received = contact;
    };
    
    // Queue mock response
    var mockResponse = CreateMockContactResponse();
    connection.QueueResponse(mockResponse);
    
    // Trigger operation
    connection.sendCommandGetContacts();
    
    // Verify
    Assert.IsNotNull(received);
    Assert.AreEqual("TestContact", received.AdvName);
}
```

## Performance Considerations

- **Frame Processing**: Events are synchronous; keep handlers fast
- **Memory**: Payload objects are records (immutable, allocated per event)
- **Threading**: No built-in async support yet; wrap in Tasks if needed
- **Network**: Buffer sizes limited by protocol (varies by command)

## Future Enhancements

The following features are planned for future releases:

1. **Async/Await Support**
   - Convert blocking operations to async
   - Implement Task-based API alongside events

2. **Connection Pooling**
   - Support multiple simultaneous connections
   - Connection state management

3. **Enhanced Logging**
   - Configurable logging levels
   - Frame-level debugging support

4. **Higher-Level APIs**
   - Conversation management
   - Contact groups
   - Message threading

5. **Platform-Specific Implementations**
   - Pre-built BLE implementations for major platforms
   - Serial port utilities
   - Network bridge examples

---

**Document Version**: 1.0  
**Last Updated**: 2024  
**Library Version**: 1.0.0
