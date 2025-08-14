using System.Diagnostics;

namespace meshcore_lib;

public class Constants {
    public const int SupportedCompanionProtocolVersion = 1;

    public enum SerialFrameTypes {
        Incoming = 0x3e, // ">"
        Outgoing = 0x3c, // "<"
    }

    public enum Ble {
        ServiceUuid = 1,
        CharacteristicUuidRx,
        CharacteristicUuidTx,
    }

    public static string BleToUuid(Ble ble) {
        return ble switch {
            Ble.ServiceUuid => "6E400001-B5A3-F393-E0A9-E50E24DCCA9E",
            Ble.CharacteristicUuidRx => "6E400002-B5A3-F393-E0A9-E50E24DCCA9E",
            Ble.CharacteristicUuidTx => "6E400003-B5A3-F393-E0A9-E50E24DCCA9E",
            _ => throw new UnreachableException()
        };
    }

    public static Ble UuidToBle(string uuid) {
        return uuid switch {
            "6E400001-B5A3-F393-E0A9-E50E24DCCA9E" => Ble.ServiceUuid,
            "6E400002-B5A3-F393-E0A9-E50E24DCCA9E" => Ble.CharacteristicUuidRx,
            "6E400003-B5A3-F393-E0A9-E50E24DCCA9E" => Ble.CharacteristicUuidTx,
            _ => throw new ArgumentOutOfRangeException(nameof(uuid), uuid, null)
        };
    }

    public enum CommandCodes {
        AppStart,
        SendTxtMsg,
        SendChannelTxtMsg,
        GetContacts,
        GetDeviceTime,
        SetDeviceTime,
        SendSelfAdvert,
        SetAdvertName,
        AddUpdateContact,
        SyncNextMessage,
        SetRadioParams,
        SetTxPower,
        ResetPath,
        SetAdvertLatLon,
        RemoveContact,
        ShareContact,
        ExportContact,
        ImportContact,
        Reboot,
        GetBatteryVoltage,
        SetTuningParams, 
        // todo
        DeviceQuery,
        ExportPrivateKey,
        ImportPrivateKey,
        SendRawData,
        SendLogin, 
        // todo
        SendStatusReq, 
        // todo
        GetChannel,
        SetChannel, 
        // todo sign commands
        SendTracePath, 
        // todo set device pin command
        SetOtherParams,
        SendTelemetryReq,
    }

    public static byte GetCommandCodeByte(CommandCodes code) {
        return code switch {
            CommandCodes.AppStart => 1,
            CommandCodes.SendTxtMsg => 2,
            CommandCodes.SendChannelTxtMsg => 3,
            CommandCodes.GetContacts => 4,
            CommandCodes.GetDeviceTime => 5,
            CommandCodes.SetDeviceTime => 6,
            CommandCodes.SendSelfAdvert => 7,
            CommandCodes.SetAdvertName => 8,
            CommandCodes.AddUpdateContact => 9,
            CommandCodes.SyncNextMessage => 10,
            CommandCodes.SetRadioParams => 11,
            CommandCodes.SetTxPower => 12,
            CommandCodes.ResetPath => 13,
            CommandCodes.SetAdvertLatLon => 14,
            CommandCodes.RemoveContact => 15,
            CommandCodes.ShareContact => 16,
            CommandCodes.ExportContact => 17,
            CommandCodes.ImportContact => 18,
            CommandCodes.Reboot => 19,
            CommandCodes.GetBatteryVoltage => 20,
            CommandCodes.SetTuningParams => 21,
            CommandCodes.DeviceQuery => 22,
            CommandCodes.ExportPrivateKey => 23,
            CommandCodes.ImportPrivateKey => 24,
            CommandCodes.SendRawData => 25,
            CommandCodes.SendLogin => 26,
            CommandCodes.SendStatusReq => 27,
            CommandCodes.GetChannel => 31,
            CommandCodes.SetChannel => 32,
            CommandCodes.SendTracePath => 36,
            CommandCodes.SetOtherParams => 38,
            CommandCodes.SendTelemetryReq => 39,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    } 
    
    public enum ResponseCodes {
        Ok = 0, // todo
        Err = 1, // todo
        ContactsStart = 2,
        Contact = 3,
        EndOfContacts = 4,
        SelfInfo = 5,
        Sent = 6,
        ContactMsgRecv = 7,
        ChannelMsgRecv = 8,
        CurrTime = 9,
        NoMoreMessages = 10,
        ExportContact = 11,
        BatteryVoltage = 12,
        DeviceInfo = 13,
        PrivateKey = 14,
        Disabled = 15,
        ChannelInfo = 18,
    }

    public static byte GetResponseCode(ResponseCodes code) {
        return code switch {
            ResponseCodes.Ok => 0,
            ResponseCodes.Err => 1,
            ResponseCodes.ContactsStart => 2,
            ResponseCodes.Contact => 3,
            ResponseCodes.EndOfContacts => 4,
            ResponseCodes.SelfInfo => 5,
            ResponseCodes.Sent => 6,
            ResponseCodes.ContactMsgRecv => 7,
            ResponseCodes.ChannelMsgRecv => 8,
            ResponseCodes.CurrTime => 9,
            ResponseCodes.NoMoreMessages => 10,
            ResponseCodes.ExportContact => 11,
            ResponseCodes.BatteryVoltage => 12,
            ResponseCodes.DeviceInfo => 13,
            ResponseCodes.PrivateKey => 14,
            ResponseCodes.Disabled => 15,
            ResponseCodes.ChannelInfo => 18,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }

    public enum PushCodes {
        Advert = 0x80, // when companion is set to auto add contacts
        PathUpdated = 0x81,
        SendConfirmed = 0x82,
        MsgWaiting = 0x83,
        RawData = 0x84,
        LoginSuccess = 0x85,
        LoginFail = 0x86, // not usable yet
        StatusResponse = 0x87,
        LogRxData = 0x88,
        TraceData = 0x89,
        NewAdvert = 0x8A, // when companion is set to manually add contacts
        TelemetryResponse = 0x8B,
    }

    public static byte GetPushCode(PushCodes code) {
        return code switch {
            PushCodes.Advert => 0x80,
            PushCodes.PathUpdated => 0x81,
            PushCodes.SendConfirmed => 0x82,
            PushCodes.MsgWaiting => 0x83,
            PushCodes.RawData => 0x84,
            PushCodes.LoginSuccess => 0x85,
            PushCodes.LoginFail => 0x86,
            PushCodes.StatusResponse => 0x87,
            PushCodes.LogRxData => 0x88,
            PushCodes.TraceData => 0x89,
            PushCodes.NewAdvert => 0x8A,
            PushCodes.TelemetryResponse => 0x8B,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }

    public enum ErrorCodes {
        UnsupportedCmd = 1,
        NotFound = 2,
        TableFull = 3,
        BadState = 4,
        FileIoError = 5,
        IllegalArg = 6,
    }

    public enum AdvType {
        None = 0,
        Chat = 1,
        Repeater = 2,
        Room = 3,
    }

    public enum SelfAdvertTypes {
        ZeroHop = 0,
        Flood = 1,
    }

    public enum TxtType {
        Plain = 0,
        CliData = 1,
        SignedPlain = 2,
    }

    public static byte GetTxtTypeByte(TxtType txtType) {
        return txtType switch {
            TxtType.Plain => 0,
            TxtType.CliData => 1,
            TxtType.SignedPlain => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(txtType), txtType, null)
        };
    }

    public static byte GetAdvTypeByte(AdvType advType) {
        return advType switch {
            AdvType.None => 0,
            AdvType.Chat => 1,
            AdvType.Repeater => 2,
            AdvType.Room => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(advType), advType, null)
        };
    }

    public static byte GetSelfAdvertByte(SelfAdvertTypes selfAdvertType) {
        return selfAdvertType switch {
            SelfAdvertTypes.ZeroHop => 0,
            SelfAdvertTypes.Flood => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(selfAdvertType), selfAdvertType, null)
        };
    }
}