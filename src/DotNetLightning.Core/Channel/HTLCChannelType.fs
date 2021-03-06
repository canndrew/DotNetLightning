namespace DotNetLightning.Channel
open NBitcoin
open DotNetLightning.Utils
open DotNetLightning.Serialize.Msgs
open DotNetLightning.Utils.OnionError

type PendingForwardHTLCInfo = {
    OnionPacket: OnionPacket option
    IncomingSharedSecret: uint8
    PaymentHash: uint256
    ShortChannelId: ShortChannelId
    AmountToForward: LNMoney
    OutgoingCLTVValue: uint32
}

type HTLCFailureMsg =
    | Relay of UpdateFailHTLCMsg
    | Malformed of UpdateFailMalformedHTLCMsg

type PendingHTLCStatus =
    | Forward of PendingForwardHTLCInfo

type HTLCPreviousHopData =
    | ShortChannelId of ShortChannelId
    | HTLCId of HTLCId
    | IncomingPacketSharedSecret of PaymentPreimage

type OutboundRoute = {
    Route: Route
    SessionPriv: Key
    FirstHopHTLC: LNMoney
}

type HTLCSource =
    | PreviousHopData of HTLCPreviousHopData
    | OutboundRoute of OutboundRoute

type HTLCFailureData = {
    Data: byte[]
    FailureCode: FailureCode
}

type HTLCFailReason =
    | ErrorPacket of OnionErrorPacket
    | Reason of HTLCFailureData
