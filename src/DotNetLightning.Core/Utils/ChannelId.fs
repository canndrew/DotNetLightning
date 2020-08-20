namespace DotNetLightning.Utils

open NBitcoin

type ChannelId = {
    RawId: uint256
} with
    static member FromRawId (rawId: uint256): ChannelId = { RawId = rawId }
    static member Zero = uint256.Zero |> ChannelId.FromRawId

