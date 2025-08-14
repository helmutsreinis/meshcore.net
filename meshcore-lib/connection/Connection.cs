using meshcore_lib.connection.payloads;
using meshcore_lib.utils;

namespace meshcore_lib.connection;

public abstract class Connection {
    protected abstract void Close();
    protected abstract void SendToRadioFrame(byte[] data);
    private readonly Lock _rwLock = new();

    public event EventHandler Connected;
    public event EventHandler<byte[]> FrameReceived;
    public event EventHandler<byte[]> AdvertPush;
    public event EventHandler<byte[]> PathUpdatedPush;
    public event EventHandler<SendConfirmedPushPayload> SendConfirmedPush;
    public event EventHandler MsgWaitingPush;
    public event EventHandler<RawDataPushPayload> RawDataPush;
    public event EventHandler<LoginSuccessPushPayload> LoginSuccessPush;
    public event EventHandler<StatusResponsePushPayload> StatusResponsePush;
    public event EventHandler<LogRxDataPushPayload> LogRxDataPush;
    public event EventHandler<TelemetryResponsePushPayload> TelemetryResponsePush;
    public event EventHandler<TraceDataPushPayload> TraceDataPush;
    public event EventHandler<NewAdvertPushPayload> NewAdvertPush;
    public event EventHandler OkResponse;
    public event EventHandler ErrResponse;
    public event EventHandler<uint> ContactsStartResponse;
    public event EventHandler<ContactPayload> ContactResponse;
    public event EventHandler<uint> EndOfContactsResponse;
    public event EventHandler<SentPayload> SentResponse;
    //sender
    public event EventHandler<byte[]> ExportContactResponse;
    //mV
    public event EventHandler<ushort> BatteryVoltageResponse;
    public event EventHandler<DeviceInfoPayload> DeviceInfoResponse;
    //privateKey
    public event EventHandler<byte[]> PrivateKeyResponse;
    public event EventHandler DisabledResponse;
    public event EventHandler<ChannelInfoPayload> ChannelInfoResponse;
    public event EventHandler<SelfInfoPayload> SelfInfoResponse;
    public event EventHandler<uint> CurrentTimeResponse;
    public event EventHandler NoMoreMessagesResponse;
    public event EventHandler<ContactMsgPayload> ContactMsgRecv;
    public event EventHandler<ChannelMsgPayload> ChannelMsgRecv;


    public void onConnected() {
        // tell device what protocol version we support
        try {
            deviceQuery(Constants.SupportedCompanionProtocolVersion);
        }
        catch (Exception e) {
            // ignore
        }

        // tell clients we are connected
        Connected.Invoke(this, EventArgs.Empty);
    }

    public void sendCommandAppStart() {
        using MemoryStream ms = new();
        using BufferWriter data = new(ms);
        data.WriteByte(Constants.CommandCodes.AppStart);
        data.WriteByte(1); // appVer
        data.WriteBytes(new byte[6]); // reserved
        data.WriteString("test"); // appName
        SendToRadioFrame(ms.ToArray());
    }

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

    public void syncDeviceTime() {
        await this.setDeviceTime(Math.floor(JSType.Date.now() / 1000));
    }

    importContact(advertPacketBytes) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = (response) => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // import contact
                await this.sendCommandImportContact(advertPacketBytes);

            } catch(e) {
                reject(e);
            }
        });
    }

    exportContact(pubKey = null) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive export contact response
                const onExportContact = (response) => {
                    this.off(Constants.ResponseCodes.ExportContact, onExportContact);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.ExportContact, onExportContact);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.ExportContact, onExportContact);
                this.once(Constants.ResponseCodes.Err, onErr);

                // export contact
                await this.sendCommandExportContact(pubKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    shareContact(pubKey) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = (response) => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // share contact
                await this.sendCommandShareContact(pubKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    removeContact(pubKey) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // remove contact
                await this.sendCommandRemoveContact(pubKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    addOrUpdateContact(publicKey, type, flags, outPathLen, outPath, advName, lastAdvert, advLat, advLon) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // add or update contact
                await this.sendCommandAddUpdateContact(publicKey, type, flags, outPathLen, outPath, advName, lastAdvert, advLat, advLon);

            } catch(e) {
                reject(e);
            }
        });
    }

    setContactPath(contact, path) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // create empty out path
                const maxPathLength = 64;
                const outPath = new Uint8Array(maxPathLength);

                // fill out path with the provided path
                for(var i = 0; i < path.length && i < maxPathLength; i++){
                    outPath[i] = path[i];
                }

                // update contact details with new path and path length
                contact.outPathLen = path.length;
                contact.outPath = outPath;

                // update contact
                return await this.addOrUpdateContact(contact.publicKey, contact.type, contact.flags, contact.outPathLen, contact.outPath, contact.advName, contact.lastAdvert, contact.advLat, contact.advLon);

            } catch(e) {
                reject(e);
            }
        });
    }

    resetPath(pubKey) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // reset path
                await this.sendCommandResetPath(pubKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    reboot() {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // assume device rebooted after a short delay
                setTimeout(() => {
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }, 1000);

                // listen for events
                this.once(Constants.ResponseCodes.Err, onErr);

                // reboot
                await this.sendCommandReboot();

            } catch(e) {
                reject(e);
            }
        });
    }

    getBatteryVoltage() {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive battery voltage
                const onBatteryVoltage = (response) => {
                    this.off(Constants.ResponseCodes.BatteryVoltage, onBatteryVoltage);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.BatteryVoltage, onBatteryVoltage);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.BatteryVoltage, onBatteryVoltage);
                this.once(Constants.ResponseCodes.Err, onErr);

                // get battery voltage
                await this.sendCommandGetBatteryVoltage();

            } catch(e) {
                reject(e);
            }
        });
    }

    deviceQuery(appTargetVer) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive device info
                const onDeviceInfo = (response) => {
                    this.off(Constants.ResponseCodes.DeviceInfo, onDeviceInfo);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.DeviceInfo, onDeviceInfo);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.DeviceInfo, onDeviceInfo);
                this.once(Constants.ResponseCodes.Err, onErr);

                // query device
                await this.sendCommandDeviceQuery(appTargetVer);

            } catch(e) {
                reject(e);
            }
        });
    }

    exportPrivateKey() {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive private Key
                const onPrivateKey = (response) => {
                    this.off(Constants.ResponseCodes.PrivateKey, onPrivateKey);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.PrivateKey, onPrivateKey);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    reject();
                }

                // reject promise when we receive disabled
                const onDisabled = () => {
                    this.off(Constants.ResponseCodes.PrivateKey, onPrivateKey);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    reject("disabled");
                }

                // listen for events
                this.once(Constants.ResponseCodes.PrivateKey, onPrivateKey);
                this.once(Constants.ResponseCodes.Err, onErr);
                this.once(Constants.ResponseCodes.Disabled, onDisabled);

                // export private key
                await this.sendCommandExportPrivateKey();

            } catch(e) {
                reject(e);
            }
        });
    }

    importPrivateKey(privateKey) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = (response) => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    reject();
                }

                // reject promise when we receive disabled
                const onDisabled = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Disabled, onDisabled);
                    reject("disabled");
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);
                this.once(Constants.ResponseCodes.Disabled, onDisabled);

                // import private key
                await this.sendCommandImportPrivateKey(privateKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    login(contactPublicKey, password, extraTimeoutMillis = 1000) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // get public key prefix we expect in the login response
                const publicKeyPrefix = contactPublicKey.subarray(0, 6);

                // listen for sent response so we can get estimated timeout
                var timeoutHandler = null;
                const onSent = (response) => {

                    // remove error listener since we received sent response
                    this.once(Constants.ResponseCodes.Err, onErr);

                    // reject login request as timed out after estimated delay, plus a bit extra
                    const estTimeout = response.estTimeout + extraTimeoutMillis;
                    timeoutHandler = setTimeout(() => {
                        reject("timeout");
                    }, estTimeout);

                }

                // resolve promise when we receive login success push code
                const onLoginSuccess = (response) => {

                    // make sure login success response is for this login request
                    if(!BufferUtils.areBuffersEqual(publicKeyPrefix, response.pubKeyPrefix)){
                        console.log("onLoginSuccess is not for this login request, ignoring...");
                        return;
                    }

                    // login successful
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.LoginSuccess, onLoginSuccess);
                    resolve(response);

                }

                // reject promise when we receive err
                const onErr = () => {
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.LoginSuccess, onLoginSuccess);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Err, onErr);
                this.once(Constants.ResponseCodes.Sent, onSent);
                this.once(Constants.PushCodes.LoginSuccess, onLoginSuccess);

                // login
                await this.sendCommandSendLogin(contactPublicKey, password);

            } catch(e) {
                reject(e);
            }
        });
    }

    getStatus(contactPublicKey, extraTimeoutMillis = 1000) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // get public key prefix we expect in the status response
                const publicKeyPrefix = contactPublicKey.subarray(0, 6);

                // listen for sent response so we can get estimated timeout
                var timeoutHandler = null;
                const onSent = (response) => {

                    // remove error listener since we received sent response
                    this.once(Constants.ResponseCodes.Err, onErr);

                    // reject login request as timed out after estimated delay, plus a bit extra
                    const estTimeout = response.estTimeout + extraTimeoutMillis;
                    timeoutHandler = setTimeout(() => {
                        reject("timeout");
                    }, estTimeout);

                }

                // resolve promise when we receive status response push code
                const onStatusResponsePush = (response) => {

                    // make sure login success response is for this login request
                    if(!BufferUtils.areBuffersEqual(publicKeyPrefix, response.pubKeyPrefix)){
                        console.log("onStatusResponsePush is not for this status request, ignoring...");
                        return;
                    }

                    // status request successful
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.StatusResponse, onStatusResponsePush);

                    // parse repeater stats from status data
                    const bufferReader = new BufferReader(response.statusData);
                    const repeaterStats = {
                        batt_milli_volts: bufferReader.readUInt16LE(), // uint16_t batt_milli_volts;
                        curr_tx_queue_len: bufferReader.readUInt16LE(), // uint16_t curr_tx_queue_len;
                        curr_free_queue_len: bufferReader.readUInt16LE(), // uint16_t curr_free_queue_len;
                        last_rssi: bufferReader.readInt16LE(), // int16_t  last_rssi;
                        n_packets_recv: bufferReader.readUInt32LE(), // uint32_t n_packets_recv;
                        n_packets_sent: bufferReader.readUInt32LE(), // uint32_t n_packets_sent;
                        total_air_time_secs: bufferReader.readUInt32LE(), // uint32_t total_air_time_secs;
                        total_up_time_secs: bufferReader.readUInt32LE(), // uint32_t total_up_time_secs;
                        n_sent_flood: bufferReader.readUInt32LE(), // uint32_t n_sent_flood
                        n_sent_direct: bufferReader.readUInt32LE(), // uint32_t n_sent_direct
                        n_recv_flood: bufferReader.readUInt32LE(), // uint32_t n_recv_flood
                        n_recv_direct: bufferReader.readUInt32LE(), // uint32_t n_recv_direct
                        n_full_events: bufferReader.readUInt16LE(), // uint16_t n_full_events
                        last_snr: bufferReader.readInt16LE(), // int16_t last_snr
                        n_direct_dups: bufferReader.readUInt16LE(), // uint16_t n_direct_dups
                        n_flood_dups: bufferReader.readUInt16LE(), // uint16_t n_flood_dups
                    }

                    resolve(repeaterStats);

                }

                // reject promise when we receive err
                const onErr = () => {
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.StatusResponse, onStatusResponsePush);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Err, onErr);
                this.once(Constants.ResponseCodes.Sent, onSent);
                this.once(Constants.PushCodes.StatusResponse, onStatusResponsePush);

                // request status
                await this.sendCommandSendStatusReq(contactPublicKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    getTelemetry(contactPublicKey, extraTimeoutMillis = 1000) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // get public key prefix we expect in the telemetry response
                const publicKeyPrefix = contactPublicKey.subarray(0, 6);

                // listen for sent response so we can get estimated timeout
                var timeoutHandler = null;
                const onSent = (response) => {

                    // remove error listener since we received sent response
                    this.once(Constants.ResponseCodes.Err, onErr);

                    // reject as timed out after estimated delay, plus a bit extra
                    const estTimeout = response.estTimeout + extraTimeoutMillis;
                    timeoutHandler = setTimeout(() => {
                        reject("timeout");
                    }, estTimeout);

                }

                // resolve promise when we receive telemetry response push code
                const onTelemetryResponsePush = (response) => {

                    // make sure telemetry response is for this telemetry request
                    if(!BufferUtils.areBuffersEqual(publicKeyPrefix, response.pubKeyPrefix)){
                        console.log("onTelemetryResponsePush is not for this telemetry request, ignoring...");
                        return;
                    }

                    // telemetry request successful
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.TelemetryResponse, onTelemetryResponsePush);

                    resolve(response);

                }

                // reject promise when we receive err
                const onErr = () => {
                    clearTimeout(timeoutHandler);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.ResponseCodes.Sent, onSent);
                    this.off(Constants.PushCodes.TelemetryResponse, onTelemetryResponsePush);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Err, onErr);
                this.once(Constants.ResponseCodes.Sent, onSent);
                this.once(Constants.PushCodes.TelemetryResponse, onTelemetryResponsePush);

                // request telemetry
                await this.sendCommandSendTelemetryReq(contactPublicKey);

            } catch(e) {
                reject(e);
            }
        });
    }

    pingRepeaterZeroHop(contactPublicKey, timeoutMillis) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // create raw data using custom packet
                const bufferWriter = new BufferWriter();
                bufferWriter.writeUInt32LE(JSType.Date.now()); // timestamp millis so every ping is unique
                bufferWriter.writeBytes([0x70, 0x69, 0x6E, 0x67]); // "ping" as bytes
                bufferWriter.writeBytes(contactPublicKey.subarray(0, 2)); // 2 bytes from the repeaters public key, so we don't use another repeaters ping response
                const rawBytes = bufferWriter.toBytes();

                var startMillis = JSType.Date.now();

                // resolve promise when we receive expected response
                const onLogRxDataPush = (response) => {

                    // calculate round trip time
                    const endMillis = JSType.Date.now();
                    const durationMillis = endMillis - startMillis;

                    // parse packet from rx data, and make sure it's expected type
                    const packet = Packet.fromBytes(response.raw);
                    if(packet.payload_type !== Packet.PAYLOAD_TYPE_RAW_CUSTOM){
                        return;
                    }

                    // make sure the payload we sent, is the payload we received
                    if(!BufferUtils.areBuffersEqual(packet.payload, rawBytes)){
                        return;
                    }

                    // ping successful remove all listeners
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.PushCodes.LogRxData, onLogRxDataPush);

                    // send back results
                    resolve({
                        rtt: durationMillis,
                        snr: response.lastSnr,
                        rssi: response.lastRssi,
                    });

                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Err, onErr);
                    this.off(Constants.PushCodes.LogRxData, onLogRxDataPush);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Err, onErr);
                this.on(Constants.PushCodes.LogRxData, onLogRxDataPush);

                // check if a timeout was provided
                if(timeoutMillis != null){
                    setTimeout(() => {

                        // stop listening for events
                        this.off(Constants.ResponseCodes.Err, onErr);
                        this.off(Constants.PushCodes.LogRxData, onLogRxDataPush);

                        // reject since it timed out
                        reject("timeout");

                    }, timeoutMillis);
                }

                // send raw data to repeater, for it to repeat zero hop
                await this.sendCommandSendRawData([
                    // we set the repeater we want to ping as the path
                    // it should repeat our packet, and we can listen for it
                    contactPublicKey.subarray(0, 1),
                ], rawBytes);

            } catch(e) {
                reject(e);
            }
        });
    }

    getChannel(channelIdx) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive channel info response
                const onChannelInfoResponse = (response) => {
                    this.off(Constants.ResponseCodes.ChannelInfo, onChannelInfoResponse);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.ChannelInfo, onChannelInfoResponse);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.ChannelInfo, onChannelInfoResponse);
                this.once(Constants.ResponseCodes.Err, onErr);

                // get channel
                await this.sendCommandGetChannel(channelIdx);

            } catch(e) {
                reject(e);
            }
        });
    }

    setChannel(channelIdx, name, secret) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // set channel
                await this.sendCommandSetChannel(channelIdx, name, secret);

            } catch(e) {
                reject(e);
            }
        });
    }

    async deleteChannel(channelIdx) {
        return await this.setChannel(channelIdx, "", new Uint8Array(16));
    }

    getChannels() {
        return new JSType.Promise<>(async (resolve, reject) => {

            // get channels until we get an error
            var channelIdx = 0;
            const channels = [];
            while(true){

                // try to get next channel
                try {
                    const channel = await this.getChannel(channelIdx);
                    channels.push(channel);
                } catch(e){
                    break;
                }

                channelIdx++;

            }

            return resolve(channels);

        });
    }

    async findChannelByName(name) {

        // get channels
        const channels = await this.getChannels();

        // find first channel matching name exactly
        return channels.find((channel) => {
            console.log(channel);
            return channel.name === name;
        });

    }

    async findChannelBySecret(secret) {

        // get channels
        const channels = await this.getChannels();

        // find first channel matching secret
        return channels.find((channel) => {
            return BufferUtils.areBuffersEqual(secret, channel.secret);
        });

    }

    tracePath(path) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // generate a random tag for this trace, so we can listen for the correct response
                const tag = RandomUtils.getRandomInt(0, 4294967295);

                // resolve promise when we receive trace data
                const onTraceDataPush = (response) => {

                    // make sure tag matches
                    if(response.tag !== tag){
                        console.log("ignoring trace data for a different trace request");
                        return;
                    }

                    // resolve
                    this.off(Constants.PushCodes.TraceData, onTraceDataPush);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve(response);

                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.PushCodes.TraceData, onTraceDataPush);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.on(Constants.PushCodes.TraceData, onTraceDataPush);
                this.once(Constants.ResponseCodes.Err, onErr);

                // trace path
                await this.sendCommandSendTracePath(tag, 0, path);

            } catch(e) {
                reject(e);
            }
        });
    }

    setOtherParams(manualAddContacts) {
        return new JSType.Promise<>(async (resolve, reject) => {
            try {

                // resolve promise when we receive ok
                const onOk = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    resolve();
                }

                // reject promise when we receive err
                const onErr = () => {
                    this.off(Constants.ResponseCodes.Ok, onOk);
                    this.off(Constants.ResponseCodes.Err, onErr);
                    reject();
                }

                // listen for events
                this.once(Constants.ResponseCodes.Ok, onOk);
                this.once(Constants.ResponseCodes.Err, onErr);

                // set other params
                await this.sendCommandSetOtherParams(manualAddContacts);

            } catch(e) {
                reject(e);
            }
        });
    }

    async setAutoAddContacts() {
        return await this.setOtherParams(false);
    }

    async setManualAddContacts() {
        return await this.setOtherParams(true);
    }

}