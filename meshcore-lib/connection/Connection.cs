using meshcore_lib.connection.payloads;
using meshcore_lib.utils;

namespace meshcore_lib.connection;

/// <summary>
/// Abstract base class for Meshcore device connections.
/// Provides the core functionality for communicating with Meshcore mesh network devices
/// through an event-driven architecture.
/// </summary>
/// <remarks>
/// <para>
/// Implement this class to create connections for specific transport layers (BLE, Serial, etc.).
/// You must implement the <see cref="Close"/> and <see cref="SendToRadioFrame"/> methods.
/// </para>
/// <para>
/// The class follows an event-driven pattern:
/// - Send commands using sendCommand* methods
/// - Receive responses by subscribing to event handlers
/// - Handle push notifications asynchronously
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class BleConnection : Connection
/// {
///     protected override void Close()
///     {
///         // Close your BLE connection
///     }
///     
///     protected override void SendToRadioFrame(byte[] data)
///     {
///         // Write data to BLE characteristic
///     }
/// }
/// </code>
/// </example>
public abstract class Connection {
    /// <summary>
    /// Closes the connection to the device. Implement this to clean up your transport resources.
    /// </summary>
    protected abstract void Close();
    
    /// <summary>
    /// Sends a data frame to the Meshcore device.
    /// Implement this to write data to your specific transport layer (BLE, Serial, etc.).
    /// </summary>
    /// <param name="data">The binary data frame to send to the device</param>
    protected abstract void SendToRadioFrame(byte[] data);
    
    private readonly Lock _rwLock = new();

    // Push Events (Asynchronous notifications from device)
    
    /// <summary>Raised when the connection to the device is established</summary>
    public event EventHandler Connected;
    
    /// <summary>Raised when a raw frame is received from the device</summary>
    public event EventHandler<byte[]> FrameReceived;
    
    /// <summary>Raised when an advertisement is received from another node (auto-add mode)</summary>
    public event EventHandler<byte[]> AdvertPush;
    
    /// <summary>Raised when a contact's path has been updated</summary>
    public event EventHandler<byte[]> PathUpdatedPush;
    
    /// <summary>Raised when a sent message has been confirmed</summary>
    public event EventHandler<SendConfirmedPushPayload> SendConfirmedPush;
    
    /// <summary>Raised when there are messages waiting to be retrieved</summary>
    public event EventHandler MsgWaitingPush;
    
    /// <summary>Raised when raw data is received from the mesh network</summary>
    public event EventHandler<RawDataPushPayload> RawDataPush;
    
    /// <summary>Raised when login to a repeater or room is successful</summary>
    public event EventHandler<LoginSuccessPushPayload> LoginSuccessPush;
    
    /// <summary>Raised when a status response is received from a repeater</summary>
    public event EventHandler<StatusResponsePushPayload> StatusResponsePush;
    
    /// <summary>Raised when logged RX data is available</summary>
    public event EventHandler<LogRxDataPushPayload> LogRxDataPush;
    
    /// <summary>Raised when telemetry response is received from a node</summary>
    public event EventHandler<TelemetryResponsePushPayload> TelemetryResponsePush;
    
    /// <summary>Raised when trace data is received for a path trace operation</summary>
    public event EventHandler<TraceDataPushPayload> TraceDataPush;
    
    /// <summary>Raised when a new advertisement is received (manual-add mode)</summary>
    public event EventHandler<NewAdvertPushPayload> NewAdvertPush;
    
    // Response Events (Responses to commands)
    
    /// <summary>Raised when a command completes successfully</summary>
    public event EventHandler OkResponse;
    
    /// <summary>Raised when a command fails</summary>
    public event EventHandler ErrResponse;
    
    /// <summary>Raised when contact synchronization starts, provides total count</summary>
    public event EventHandler<uint> ContactsStartResponse;
    
    /// <summary>Raised for each contact during synchronization</summary>
    public event EventHandler<ContactPayload> ContactResponse;
    
    /// <summary>Raised when contact synchronization completes</summary>
    public event EventHandler<uint> EndOfContactsResponse;
    
    /// <summary>Raised when a message has been queued for transmission</summary>
    public event EventHandler<SentPayload> SentResponse;
    
    /// <summary>Raised when contact export data is available (raw advertisement packet)</summary>
    public event EventHandler<byte[]> ExportContactResponse;
    
    /// <summary>Raised when battery voltage response is received (in millivolts)</summary>
    public event EventHandler<ushort> BatteryVoltageResponse;
    
    /// <summary>Raised when device information is received</summary>
    public event EventHandler<DeviceInfoPayload> DeviceInfoResponse;
    
    /// <summary>Raised when private key export data is available (64 bytes)</summary>
    public event EventHandler<byte[]> PrivateKeyResponse;
    
    /// <summary>Raised when a feature is disabled on the device</summary>
    public event EventHandler DisabledResponse;
    
    /// <summary>Raised when channel information is received</summary>
    public event EventHandler<ChannelInfoPayload> ChannelInfoResponse;
    
    /// <summary>Raised when device self-information is received</summary>
    public event EventHandler<SelfInfoPayload> SelfInfoResponse;
    
    /// <summary>Raised when current device time is received (Unix epoch seconds)</summary>
    public event EventHandler<uint> CurrentTimeResponse;
    
    /// <summary>Raised when there are no more messages to sync</summary>
    public event EventHandler NoMoreMessagesResponse;
    
    /// <summary>Raised when a contact message is received</summary>
    public event EventHandler<ContactMsgPayload> ContactMsgRecv;
    
    /// <summary>Raised when a channel message is received</summary>
    public event EventHandler<ChannelMsgPayload> ChannelMsgRecv;


    /// <summary>
    /// Call this method when the connection to the device is established.
    /// This will send the protocol version to the device and raise the Connected event.
    /// </summary>
    /// <example>
    /// <code>
    /// // In your BLE connection implementation:
    /// protected override void OnBleConnected()
    /// {
    ///     connection.onConnected();
    /// }
    /// </code>
    /// </example>
    public void onConnected() {
        // tell device what protocol version we support
        try {
            sendCommandDeviceQuery(Constants.SupportedCompanionProtocolVersion);
        }
        catch (Exception) {
            // ignore
        }

        // tell clients we are connected
        Connected.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sends the AppStart command to initialize the app connection with the device.
    /// </summary>
    /// <remarks>
    /// This is typically one of the first commands sent after connection.
    /// It identifies the app and version to the device.
    /// </remarks>
    public void sendCommandAppStart() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.AppStart);
        data.WriteByte(1); // appVer
        data.WriteBytes(new byte[6]); // reserved
        data.WriteString("test"); // appName
        SendToRadioFrame(ms.ToArray());
    }

    /// <summary>
    /// Sends a text message to a contact through the mesh network.
    /// </summary>
    /// <param name="txtType">The type of text message (Plain, CliData, or SignedPlain)</param>
    /// <param name="attempt">Retry attempt number (usually 0 for first attempt)</param>
    /// <param name="senderTimestamp">Unix timestamp when the message was created</param>
    /// <param name="pubKeyPrefix">Public key of the recipient contact (only first 6 bytes used)</param>
    /// <param name="text">The message text to send</param>
    /// <remarks>
    /// Subscribe to <see cref="SentResponse"/> to get confirmation that the message was queued.
    /// Subscribe to <see cref="SendConfirmedPush"/> to know when delivery is confirmed.
    /// </remarks>
    /// <example>
    /// <code>
    /// var timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    /// connection.sendCommandSendTxtMsg(
    ///     Constants.TxtType.Plain,
    ///     0,
    ///     timestamp,
    ///     contact.PublicKey,
    ///     "Hello!"
    /// );
    /// </code>
    /// </example>
    public void sendCommandSendTxtMsg(Constants.TxtType txtType,
        byte attempt,
        uint senderTimestamp,
        byte[] pubKeyPrefix,
        string text) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendTxtMsg);
        data.WriteByte(Constants.GetTxtTypeByte(txtType));
        data.WriteByte(attempt);
        data.WriteUInt32LE(senderTimestamp);
        data.WriteBytes(pubKeyPrefix[..6]); // only the first 6 bytes of pubKey are sent
        data.WriteString(text);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendChannelTxtMsg(Constants.TxtType txtType,
        byte channelIdx,
        uint senderTimestamp,
        string text) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendChannelTxtMsg);
        data.WriteByte(txtType);
        data.WriteByte(channelIdx);
        data.WriteUInt32LE(senderTimestamp);
        data.WriteString(text);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandGetContacts(uint? since = null) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.GetContacts);
        if (since != null) {
            data.WriteUInt32LE(since.Value);
        }

        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandGetDeviceTime() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.GetDeviceTime);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetDeviceTime(uint epochSecs) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetDeviceTime);
        data.WriteUInt32LE(epochSecs);
        SendToRadioFrame(ms.ToArray());
    }


    public void sendCommandSendSelfAdvert(Constants.SelfAdvertTypes type) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendSelfAdvert);
        data.WriteByte(type);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetAdvertName(string name) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetAdvertName);
        data.WriteString(name);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandAddUpdateContact(byte[] publicKey,
        Constants.AdvType type,
        byte flags,
        byte outPathLen,
        byte[] outPath,
        string advName,
        uint lastAdvert,
        uint advLat,
        uint advLon) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.AddUpdateContact);
        data.WriteBytes(publicKey);
        data.WriteByte(type);
        data.WriteByte(flags);
        data.WriteByte(outPathLen); // todo writeInt8
        data.WriteBytes(outPath); // 64 bytes
        data.WriteCString(advName, 32); // 32 bytes
        data.WriteUInt32LE(lastAdvert);
        data.WriteUInt32LE(advLat);
        data.WriteUInt32LE(advLon);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSyncNextMessage() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SyncNextMessage);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetRadioParams(uint radioFreq, uint radioBw, byte radioSf, byte radioCr) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetRadioParams);
        data.WriteUInt32LE(radioFreq);
        data.WriteUInt32LE(radioBw);
        data.WriteByte(radioSf);
        data.WriteByte(radioCr);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetTxPower(byte txPower) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetTxPower);
        data.WriteByte(txPower);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandResetPath(byte[] pubKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ResetPath);
        data.WriteBytes(pubKey); // 32 bytes
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetAdvertLatLon(int lat, int lon) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetAdvertLatLon);
        data.WriteInt32LE(lat);
        data.WriteInt32LE(lon);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandRemoveContact(byte[] pubKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.RemoveContact);
        data.WriteBytes(pubKey); // 32 bytes
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandShareContact(byte[] pubKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ShareContact);
        data.WriteBytes(pubKey); // 32 bytes
        SendToRadioFrame(ms.ToArray());
    }

    // provide a public key to export that contact
    // not providing a public key will export local identity as a contact instead
    public void sendCommandExportContact(byte[]? pubKey = null) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ExportContact);
        if (pubKey != null) {
            data.WriteBytes(pubKey); // 32 bytes
        }

        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandImportContact(byte[] advertPacketBytes) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ImportContact);
        data.WriteBytes(advertPacketBytes); // raw advert packet bytes
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandReboot() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.Reboot);
        data.WriteString("reboot");
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandGetBatteryVoltage() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.GetBatteryVoltage);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandDeviceQuery(byte appTargetVer) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.DeviceQuery);
        data.WriteByte(appTargetVer); // e.g: 1
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandExportPrivateKey() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ExportPrivateKey);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandImportPrivateKey(byte[] privateKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.ImportPrivateKey);
        data.WriteBytes(privateKey);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendRawData(byte[] path, byte[] rawData) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendRawData);
        data.WriteByte((byte)path.Length);
        data.WriteBytes(path);
        data.WriteBytes(rawData);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendLogin(byte[] publicKey, string password) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendLogin);
        data.WriteBytes(publicKey); // 32 bytes - id of repeater or room server
        data.WriteString(password); // password is remainder of frame, max 15 characters
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendStatusReq(byte[] publicKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendStatusReq);
        data.WriteBytes(publicKey); // 32 bytes - id of repeater or room server
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendTelemetryReq(byte[] publicKey) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendTelemetryReq);
        data.WriteByte((byte)0); // reserved
        data.WriteByte((byte)0); // reserved
        data.WriteByte((byte)0); // reserved
        data.WriteBytes(publicKey); // 32 bytes - id of destination node
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandGetChannel(byte channelIdx) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.GetChannel);
        data.WriteByte(channelIdx);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetChannel(byte channelIdx, string name, byte[] secret) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetChannel);
        data.WriteByte(channelIdx);
        data.WriteCString(name, 32);
        data.WriteBytes(secret);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSendTracePath(uint tag, uint auth, byte[] path) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SendTracePath);
        data.WriteUInt32LE(tag);
        data.WriteUInt32LE(auth);
        data.WriteByte((byte)0); // flags
        data.WriteBytes(path);
        SendToRadioFrame(ms.ToArray());
    }

    public void sendCommandSetOtherParams(byte manualAddContacts) {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.SetOtherParams);
        data.WriteByte(manualAddContacts); // 0 or 1
        SendToRadioFrame(ms.ToArray());
    }

    public void onFrameReceived(byte[] frame) {
        // emit received frame
        FrameReceived.Invoke(this, frame);
        using MemoryStream ms = new(frame);
        using BufferReader data = new(ms);
        byte responseCode = data.ReadByte();

        if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.Ok)) {
            onOkResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.Err)) {
            onErrResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.SelfInfo)) {
            onSelfInfoResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.CurrTime)) {
            onCurrTimeResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.NoMoreMessages)) {
            onNoMoreMessagesResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.ContactMsgRecv)) {
            onContactMsgRecvResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.ChannelMsgRecv)) {
            onChannelMsgRecvResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.ContactsStart)) {
            onContactsStartResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.Contact)) {
            onContactResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.EndOfContacts)) {
            onEndOfContactsResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.Sent)) {
            onSentResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.ExportContact)) {
            onExportContactResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.BatteryVoltage)) {
            onBatteryVoltageResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.DeviceInfo)) {
            onDeviceInfoResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.PrivateKey)) {
            onPrivateKeyResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.Disabled)) {
            onDisabledResponse(data);
        }
        else if (responseCode == Constants.GetResponseCode(Constants.ResponseCodes.ChannelInfo)) {
            onChannelInfoResponse(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.Advert)) {
            onAdvertPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.PathUpdated)) {
            onPathUpdatedPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.SendConfirmed)) {
            onSendConfirmedPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.MsgWaiting)) {
            onMsgWaitingPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.RawData)) {
            onRawDataPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.LoginSuccess)) {
            onLoginSuccessPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.StatusResponse)) {
            onStatusResponsePush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.LogRxData)) {
            onLogRxDataPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.TelemetryResponse)) {
            onTelemetryResponsePush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.TraceData)) {
            onTraceDataPush(data);
        }
        else if (responseCode == Constants.GetPushCode(Constants.PushCodes.NewAdvert)) {
            onNewAdvertPush(data);
        }
        else {
            throw new InvalidDataException($"Unexpected packet type {responseCode:x}");
        }
    }

    private void onAdvertPush(BufferReader bufferReader) {
        AdvertPush.Invoke(this, bufferReader.ReadBytes(32));
    }

    private void onPathUpdatedPush(BufferReader bufferReader) {
        PathUpdatedPush.Invoke(this, bufferReader.ReadBytes(32));
    }

    private void onSendConfirmedPush(BufferReader bufferReader) {
        SendConfirmedPush.Invoke(this,
            new SendConfirmedPushPayload(bufferReader.ReadUInt32LE(), bufferReader.ReadUInt32LE()));
    }

    private void onMsgWaitingPush(BufferReader bufferReader) {
        MsgWaitingPush.Invoke(this, EventArgs.Empty);
    }

    private void onRawDataPush(BufferReader bufferReader) {
        RawDataPush.Invoke(this, new RawDataPushPayload(
            (sbyte)(bufferReader.ReadSByte() / 4),
            bufferReader.ReadSByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadRemainingBytes()
        ));
    }

    private void onLoginSuccessPush(BufferReader bufferReader) {
        LoginSuccessPush.Invoke(this, new LoginSuccessPushPayload(
            bufferReader.ReadByte(),
            bufferReader.ReadBytes(6)
        ));
    }

    private void onStatusResponsePush(BufferReader bufferReader) {
        StatusResponsePush.Invoke(this, new StatusResponsePushPayload(
            bufferReader.ReadByte(),
            bufferReader.ReadBytes(6),
            bufferReader.ReadRemainingBytes()
        ));
    }

    private void onLogRxDataPush(BufferReader bufferReader) {
        LogRxDataPush.Invoke(this, new LogRxDataPushPayload(
            (sbyte)(bufferReader.ReadSByte() / 4),
            bufferReader.ReadSByte(),
            bufferReader.ReadRemainingBytes()
        ));
    }

    private void onTelemetryResponsePush(BufferReader bufferReader) {
        TelemetryResponsePush.Invoke(this, new TelemetryResponsePushPayload(
            bufferReader.ReadByte(),
            bufferReader.ReadBytes(6),
            bufferReader.ReadRemainingBytes()
        ));
    }

    private void onTraceDataPush(BufferReader bufferReader) {
        byte reserved = bufferReader.ReadByte();
        byte pathLen = bufferReader.ReadByte();
        TraceDataPush.Invoke(this, new TraceDataPushPayload(
            reserved,
            pathLen,
            bufferReader.ReadByte(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadBytes(pathLen),
            bufferReader.ReadBytes(pathLen),
            (sbyte)(bufferReader.ReadSByte() / 4)
        ));
    }

    private void onNewAdvertPush(BufferReader bufferReader) {
        NewAdvertPush.Invoke(this, new NewAdvertPushPayload(new ContactPayload(
            bufferReader.ReadBytes(32),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadSByte(),
            bufferReader.ReadBytes(64),
            bufferReader.ReadCString(32),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE()
        )));
    }

    private void onOkResponse(BufferReader bufferReader) {
        OkResponse.Invoke(this, EventArgs.Empty);
    }

    private void onErrResponse(BufferReader bufferReader) {
        ErrResponse.Invoke(this, EventArgs.Empty);
    }

    private void onContactsStartResponse(BufferReader bufferReader) {
        ContactsStartResponse.Invoke(this, bufferReader.ReadUInt32LE());
    }

    private void onContactResponse(BufferReader bufferReader) {
        ContactResponse.Invoke(this, new ContactPayload(
            bufferReader.ReadBytes(32),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadSByte(),
            bufferReader.ReadBytes(64),
            bufferReader.ReadCString(32),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE()
        ));
    }

    private void onEndOfContactsResponse(BufferReader bufferReader) {
        EndOfContactsResponse.Invoke(this, bufferReader.ReadUInt32LE());
    }

    private void onSentResponse(BufferReader bufferReader) {
        SentResponse.Invoke(this, new SentPayload(
            bufferReader.ReadSByte(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE()
        ));
    }

    private void onExportContactResponse(BufferReader bufferReader) {
        ExportContactResponse.Invoke(this,bufferReader.ReadRemainingBytes());
    }

    private void  onBatteryVoltageResponse(BufferReader bufferReader) {
        BatteryVoltageResponse.Invoke(this, bufferReader.ReadUInt16LE());
        
    }

    private void onDeviceInfoResponse(BufferReader bufferReader) {
        DeviceInfoResponse.Invoke(this, new DeviceInfoPayload(
            bufferReader.ReadSByte(),
            bufferReader.ReadBytes(6),
            bufferReader.ReadCString(12),
            bufferReader.ReadString()
        ));
    }

    private void onPrivateKeyResponse(BufferReader bufferReader) {
        PrivateKeyResponse.Invoke(this,bufferReader.ReadBytes(64));
    }

    private void onDisabledResponse(BufferReader bufferReader) {
        DisabledResponse.Invoke(this,EventArgs.Empty);
    }

    private void onChannelInfoResponse(BufferReader bufferReader) {
        byte idx = bufferReader.ReadByte();
        string name = bufferReader.ReadCString(32);
        byte[] secret = bufferReader.ReadBytes(16);
        ChannelInfoResponse.Invoke(this,new ChannelInfoPayload(idx,name, secret));
    }

    private void onSelfInfoResponse(BufferReader bufferReader) {
        SelfInfoResponse.Invoke(this, new SelfInfoPayload(
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadBytes(32),
            bufferReader.ReadInt32LE(),
            bufferReader.ReadInt32LE(),
            bufferReader.ReadBytes(3),
            bufferReader.ReadByte(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadString()
        ));
    }

    private void onCurrTimeResponse(BufferReader bufferReader) {
        CurrentTimeResponse.Invoke(this, bufferReader.ReadUInt32LE());
    }

    private void onNoMoreMessagesResponse(BufferReader bufferReader) {
        NoMoreMessagesResponse.Invoke(this,EventArgs.Empty);
    }

    private void onContactMsgRecvResponse(BufferReader bufferReader) {
        ContactMsgRecv.Invoke(this,new ContactMsgPayload(
            bufferReader.ReadBytes(6),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadString()
            ));
    }

    private void onChannelMsgRecvResponse(BufferReader bufferReader) {
        ChannelMsgRecv.Invoke(this,new ChannelMsgPayload(
            bufferReader.ReadSByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadByte(),
            bufferReader.ReadUInt32LE(),
            bufferReader.ReadString()
            ));
    }

    public SelfInfoPayload getSelfInfo(int? timeoutMilis = null) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            SelfInfoPayload? ret = null;

            void OnSelfInfoResponse(object? sender, SelfInfoPayload payload) {
                ret = payload;
                SelfInfoResponse -= OnSelfInfoResponse;
                e.Set();
            }

            SelfInfoResponse += OnSelfInfoResponse;

            sendCommandAppStart();
            if (timeoutMilis == null) {
                e.WaitOne();
            }
            else {
                e.WaitOne(timeoutMilis.Value);
            }

            return ret ?? throw new IOException("SelfInfoPayload is missing!;");
        }
    }

    private void sendAdvert(Constants.SelfAdvertTypes type) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSendSelfAdvert(type);
            e.WaitOne();
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public void sendFloodAdvert() {
        sendAdvert(Constants.SelfAdvertTypes.Flood);
    }

    public void sendZeroHopAdvert() {
        sendAdvert(Constants.SelfAdvertTypes.ZeroHop);
    }

    public void setAdvertName(string name) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSetAdvertName(name);
            e.WaitOne();
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public void setAdvertLatLong(int latitude,int longitude) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSetAdvertLatLon(latitude, longitude);
            e.WaitOne();
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public void setTxPower(byte txPower) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSetTxPower(txPower);
            e.WaitOne();
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public void setRadioParams(uint radioFreq, uint radioBw, byte radioSf, byte radioCr) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSetRadioParams(radioFreq, radioBw, radioSf, radioCr);
            e.WaitOne();
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public List<ContactPayload> getContacts() {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            List<ContactPayload> payloads = [];

            void OnContact(object? sender, ContactPayload payload) {
                payloads.Add(payload);
            }

            void OnEnd(object? sender, uint payload) {
                ContactResponse -= OnContact;
                EndOfContactsResponse -= OnEnd;
                e.Set();
            }

            ContactResponse += OnContact;
            EndOfContactsResponse += OnEnd;

            sendCommandGetContacts();
            e.WaitOne();
            return payloads;
        }
    }

    public ContactPayload? findContactByName(string name) {
        // get contacts
        List<ContactPayload> contacts = getContacts();

        // find first contact matching name exactly
        return contacts.Find(contact => contact.AdvName == name);
    }

    public ContactPayload? findContactByPublicKeyPrefix(byte[] pubKeyPrefix) {
        // get contacts
        List<ContactPayload> contacts = getContacts();

        // find first contact matching pub key prefix
        return contacts.Find((contact) => {
            byte[] contactPubKeyPrefix = contact.PublicKey[..pubKeyPrefix.Length];
            return pubKeyPrefix.SequenceEqual(contactPubKeyPrefix);
        });

    }

    public SentPayload? sendTextMessage(byte[] contactPublicKey, string text, Constants.TxtType txtType) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            SentPayload? res = null;

            void OnSent(object? sender, SentPayload payload) {
                SentResponse -= OnSent;
                ErrResponse -= OnErr;
                res = payload;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                SentResponse -= OnSent;
                ErrResponse -= OnErr;
                res = null;
                e.Set();
            }

            SentResponse += OnSent;
            ErrResponse += OnErr;

            sendCommandSendTxtMsg(txtType, 0, CurrentTimestamp(), contactPublicKey, text);
            e.WaitOne();
            
            return res;
        }
    }

    private static uint CurrentTimestamp() {
        return (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void sendChannelTextMessage(byte channelIdx, string text) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSendChannelTxtMsg(Constants.TxtType.Plain, channelIdx, CurrentTimestamp(), text);
            e.WaitOne();
            
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    public MsgPayload? syncNextMessage() {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            MsgPayload? res = null;

            void OnContact(object? sender, ContactMsgPayload payload) {
                ContactMsgRecv -= OnContact;
                ChannelMsgRecv -= OnChannel;
                NoMoreMessagesResponse -= OnNoMoreMessages;
                res = payload.Generalise();
                e.Set();
            }
            void OnChannel(object? sender, ChannelMsgPayload payload) {
                ContactMsgRecv -= OnContact;
                ChannelMsgRecv -= OnChannel;
                NoMoreMessagesResponse -= OnNoMoreMessages;
                res = payload.Generalise();
                e.Set();
            }

            void OnNoMoreMessages(object? sender, EventArgs payload) {
                ContactMsgRecv -= OnContact;
                ChannelMsgRecv -= OnChannel;
                NoMoreMessagesResponse -= OnNoMoreMessages;
                res = null;
                e.Set();
            }

            ContactMsgRecv += OnContact;
            ChannelMsgRecv += OnChannel;
            NoMoreMessagesResponse += OnNoMoreMessages;

            sendCommandSyncNextMessage();
            e.WaitOne();
            
            return res;
        }
    }

    public Queue<MsgPayload> getWaitingMessages() {
        Queue<MsgPayload> msgs = new();
        while(true){
            // get next message, otherwise stop if nothing is returned
            MsgPayload? msg = syncNextMessage();
            if(msg == null){
                break;
            }
            
            msgs.Enqueue(msg);
        }

        return msgs;
    }

    public uint? getDeviceTime() {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            uint? res = null;

            void OnTimeResponse(object? sender, uint payload) {
                CurrentTimeResponse -= OnTimeResponse;
                ErrResponse -= OnErr;
                res = payload;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                CurrentTimeResponse -= OnTimeResponse;
                ErrResponse -= OnErr;
                res = null;
                e.Set();
            }

            CurrentTimeResponse += OnTimeResponse;
            ErrResponse += OnErr;

            sendCommandGetDeviceTime();
            e.WaitOne();

            return res;
        }
    }

    public void setDeviceTime(uint epochSecs) {
        lock (_rwLock) {
            AutoResetEvent e = new(false);
            bool success = false;

            void OnOk(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = true;
                e.Set();
            }

            void OnErr(object? sender, EventArgs payload) {
                OkResponse -= OnOk;
                ErrResponse -= OnErr;
                success = false;
                e.Set();
            }

            OkResponse += OnOk;
            ErrResponse += OnErr;

            sendCommandSetDeviceTime(epochSecs);
            e.WaitOne();
            
            if (!success) {
                throw new IOException("Operation failed!");
            }
        }
    }

    // Note: The following methods from the JavaScript version are not yet implemented in C#:
    // - syncDeviceTime, importContact, exportContact, shareContact, removeContact
    // - addOrUpdateContact, setContactPath, resetPath, reboot, getBatteryVoltage
    // - deviceQuery, exportPrivateKey, importPrivateKey, login, getStatus
    // - getTelemetry, pingRepeaterZeroHop, getChannel, setChannel, deleteChannel
    // - getChannels, findChannelByName, findChannelBySecret, tracePath, setOtherParams
    // - setAutoAddContacts, setManualAddContacts
    // 
    // These will be implemented in future versions following C# async/await patterns.
}
