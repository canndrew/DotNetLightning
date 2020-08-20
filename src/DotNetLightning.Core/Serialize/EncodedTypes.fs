namespace DotNetLightning.Serialize

open System.IO

type QueryFlags = private {
    BitFlags: uint8
} with
    static member Create (data: uint8): QueryFlags = { BitFlags = data }
    static member TryCreate(data: uint64) =
        if data > 0xfcUL then
            Error(sprintf "Too large query flag! It must be represented as 1 byte, but it was %A" data)
        else
            Ok { BitFlags = uint8 data }
    member this.RequiresChannelAnnouncement =
        (this.BitFlags &&& 0b00000001uy) = 1uy
        
    member this.RequiresChannelUpdateForNode1 =
        (this.BitFlags &&& 0b00000010uy) = 1uy
        
    member this.RequiresChannelUpdateForNode2 =
        (this.BitFlags &&& 0b00000100uy) = 1uy
    member this.RequiresNodeAnnouncementForNode1 =
        (this.BitFlags &&& 0b00001000uy) = 1uy
    member this.RequiresNodeAnnouncementForNode2 =
        (this.BitFlags &&& 0b00010000uy) = 1uy
        
    member this.ToBytes() =
        [|(byte)this.BitFlags|]
        
type QueryOption = private {
    BitFlags: uint8
} with
    static member Create (data: uint8): QueryOption = { BitFlags = data }
    static member TryCreate(data: uint64) =
        if data > 0xfcUL then
            Error(sprintf "Too large query flag! It must be represented as 1 byte, but it was %A" data)
        else
            QueryFlags.Create(uint8 data) |> Ok
    member this.SenderWantsTimestamps =
        (this.BitFlags &&& 0b00000001uy) = 1uy
    member this.SenderWantsChecksums =
        (this.BitFlags &&& 0b00000010uy) = 1uy
    member this.ToBytes() =
        [|(byte)this.BitFlags|]
            
type TwoTimestamps = {
    NodeId1: uint32
    NodeId2: uint32
}
    with
    member this.ToBytes() =
        use ms = new MemoryStream()
        use ls = new LightningWriterStream(ms)
        ls.Write(this.NodeId1, false)
        ls.Write(this.NodeId2, false)
        ms.ToArray()

type TwoChecksums = {
    NodeId1: uint32
    NodeId2: uint32
}
    with
    member this.ToBytes() =
        use ms = new MemoryStream()
        use ls = new LightningWriterStream(ms)
        ls.Write(this.NodeId1, false)
        ls.Write(this.NodeId2, false)
        ms.ToArray()

