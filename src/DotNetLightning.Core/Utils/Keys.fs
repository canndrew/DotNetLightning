namespace DotNetLightning.Utils

open System
open NBitcoin

open ResultUtils
open ResultUtils.Portability

type FundingPubKey =
    | FundingPubKey of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (FundingPubKey pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): FundingPubKey =
        FundingPubKey <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): FundingPubKey =
        FundingPubKey <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type FundingPrivKey =
    | FundingPrivKey of Key
    with
    member this.RawKey(): Key =
        let (FundingPrivKey key) = this
        key

    member this.FundingPubKey(): FundingPubKey =
        FundingPubKey(this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): FundingPrivKey =
        FundingPrivKey <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): FundingPrivKey =
        FundingPrivKey <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type RevocationBasepoint =
    | RevocationBasepoint of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (RevocationBasepoint pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): RevocationBasepoint =
        RevocationBasepoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): RevocationBasepoint =
        RevocationBasepoint <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type RevocationBasepointSecret =
    | RevocationBasepointSecret of Key
    with
    member this.RawKey(): Key =
        let (RevocationBasepointSecret key) = this
        key

    member this.RevocationBasepoint(): RevocationBasepoint =
        RevocationBasepoint(this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): RevocationBasepointSecret =
        RevocationBasepointSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): RevocationBasepointSecret =
        RevocationBasepointSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type RevocationPubKey =
    | RevocationPubKey of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (RevocationPubKey pubKey) = this
        pubKey

    static member FromBytes(bytes: array<byte>): RevocationPubKey =
        RevocationPubKey <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): RevocationPubKey =
        RevocationPubKey <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type RevocationPrivKey =
    | RevocationPrivKey of Key
    with
    member this.RawKey(): Key =
        let (RevocationPrivKey key) = this
        key

    static member FromBytes (bytes: array<byte>): RevocationPrivKey =
        RevocationPrivKey <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): RevocationPrivKey =
        RevocationPrivKey <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type PaymentBasepoint =
    | PaymentBasepoint of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (PaymentBasepoint pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): PaymentBasepoint =
        PaymentBasepoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): PaymentBasepoint =
        PaymentBasepoint <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type PaymentBasepointSecret =
    | PaymentBasepointSecret of Key
    with
    member this.RawKey(): Key =
        let (PaymentBasepointSecret key) = this
        key

    member this.PaymentBasepoint(): PaymentBasepoint =
        PaymentBasepoint(this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): PaymentBasepointSecret =
        PaymentBasepointSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): PaymentBasepointSecret =
        PaymentBasepointSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type PaymentPubKey =
    | PaymentPubKey of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (PaymentPubKey pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): PaymentPubKey =
        PaymentPubKey <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): PaymentPubKey =
        PaymentPubKey <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type PaymentPrivKey =
    | PaymentPrivKey of Key
    with
    member this.RawKey(): Key =
        let (PaymentPrivKey key) = this
        key

    member this.PaymentPubKey(): PaymentPubKey =
        PaymentPubKey <| this.RawKey().PubKey

    static member FromBytes (bytes: array<byte>): PaymentPrivKey =
        PaymentPrivKey <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): PaymentPrivKey =
        PaymentPrivKey <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type DelayedPaymentBasepoint =
    | DelayedPaymentBasepoint of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (DelayedPaymentBasepoint pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): DelayedPaymentBasepoint =
        DelayedPaymentBasepoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): DelayedPaymentBasepoint =
        DelayedPaymentBasepoint <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type DelayedPaymentBasepointSecret =
    | DelayedPaymentBasepointSecret of Key
    with
    member this.RawKey(): Key =
        let (DelayedPaymentBasepointSecret key) = this
        key

    member this.DelayedPaymentBasepoint(): DelayedPaymentBasepoint =
        DelayedPaymentBasepoint(this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): DelayedPaymentBasepointSecret =
        DelayedPaymentBasepointSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): DelayedPaymentBasepointSecret =
        DelayedPaymentBasepointSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type DelayedPaymentPubKey =
    | DelayedPaymentPubKey of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (DelayedPaymentPubKey pubKey) = this
        pubKey

    static member FromBytes(bytes: array<byte>): DelayedPaymentPubKey =
        DelayedPaymentPubKey <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): DelayedPaymentPubKey =
        DelayedPaymentPubKey <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type DelayedPaymentPrivKey =
    | DelayedPaymentPrivKey of Key
    with
    member this.RawKey(): Key =
        let (DelayedPaymentPrivKey key) = this
        key

    member this.DelayedPaymentPubKey(): DelayedPaymentPubKey =
        DelayedPaymentPubKey <| this.RawKey().PubKey

    static member FromBytes (bytes: array<byte>): DelayedPaymentPrivKey =
        DelayedPaymentPrivKey <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): DelayedPaymentPrivKey =
        DelayedPaymentPrivKey <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type HtlcBasepoint =
    | HtlcBasepoint of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (HtlcBasepoint pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): HtlcBasepoint =
        HtlcBasepoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): HtlcBasepoint =
        HtlcBasepoint <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type HtlcBasepointSecret =
    | HtlcBasepointSecret of Key
    with
    member this.RawKey(): Key =
        let (HtlcBasepointSecret key) = this
        key

    member this.HtlcBasepoint(): HtlcBasepoint =
        HtlcBasepoint(this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): HtlcBasepointSecret =
        HtlcBasepointSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): HtlcBasepointSecret =
        HtlcBasepointSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type HtlcPubKey =
    | HtlcPubKey of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (HtlcPubKey pubKey) = this
        pubKey

    static member FromBytes (bytes: array<byte>): HtlcPubKey =
        HtlcPubKey <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): HtlcPubKey =
        HtlcPubKey <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

type HtlcPrivKey =
    | HtlcPrivKey of Key
    with
    member this.RawKey(): Key =
        let (HtlcPrivKey key) = this
        key

    member this.HtlcPubKey(): HtlcPubKey =
        HtlcPubKey <| this.RawKey().PubKey

    static member FromBytes (bytes: array<byte>): HtlcPrivKey =
        HtlcPrivKey <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): HtlcPrivKey =
        HtlcPrivKey <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type NodeSecret =
    | NodeSecret of Key
    with
    member this.RawKey(): Key =
        let (NodeSecret key) = this
        key

    member this.NodeId(): NodeId =
        NodeId (this.RawKey().PubKey)

    static member FromBytes (bytes: array<byte>): NodeSecret =
        NodeSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): NodeSecret =
        NodeSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

/// In usual operation we should not hold secrets on memory. So only hold pubkey
type ChannelPubKeys = {
    FundingPubKey: FundingPubKey
    RevocationBasepoint: RevocationBasepoint
    PaymentBasepoint: PaymentBasepoint
    DelayedPaymentBasepoint: DelayedPaymentBasepoint
    HtlcBasepoint: HtlcBasepoint
}

type CommitmentPubKeys = {
    RevocationPubKey: RevocationPubKey
    PaymentPubKey: PaymentPubKey
    DelayedPaymentPubKey: DelayedPaymentPubKey
    HtlcPubKey: HtlcPubKey
}

type PerCommitmentPoint =
    | PerCommitmentPoint of PubKey
    with
    member this.RawPubKey(): PubKey =
        let (PerCommitmentPoint pubKey) = this
        pubKey

    static member BytesLength: int = PubKey.BytesLength

    static member FromBytes(bytes: array<byte>): PerCommitmentPoint =
        PerCommitmentPoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    static member Parse (hex: string): PerCommitmentPoint =
        PerCommitmentPoint <| PubKey hex

    override this.ToString() =
        this.RawPubKey().ToString()

#if !NoDUsAsStructs
[<Struct>]
#endif
type CommitmentNumber =
    | CommitmentNumber of UInt48
    with
    member this.Index() =
        let (CommitmentNumber index) = this
        index

    override this.ToString() =
        sprintf "%012x (#%i)" (this.Index().UInt64) (UInt48.MaxValue - this.Index()).UInt64

    static member LastCommitment: CommitmentNumber =
        CommitmentNumber UInt48.Zero

    static member FirstCommitment: CommitmentNumber =
        CommitmentNumber UInt48.MaxValue

    member this.PreviousCommitment(): CommitmentNumber =
        CommitmentNumber(this.Index() + UInt48.One)

    member this.NextCommitment(): CommitmentNumber =
        CommitmentNumber(this.Index() - UInt48.One)

#if !NoDUsAsStructs
[<Struct>]
#endif
type ObscuredCommitmentNumber =
    | ObscuredCommitmentNumber of UInt48
    with
    member this.ObscuredIndex(): UInt48 =
        let (ObscuredCommitmentNumber obscuredIndex) = this
        obscuredIndex

    override this.ToString() =
        sprintf "%012x" (this.ObscuredIndex().UInt64)

type PerCommitmentSecret =
    | PerCommitmentSecret of Key
    with
    member this.RawKey(): Key =
        let (PerCommitmentSecret key) = this
        key

    static member BytesLength: int = Key.BytesLength

    static member FromBytes(bytes: array<byte>): PerCommitmentSecret =
        PerCommitmentSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    member this.PerCommitmentPoint(): PerCommitmentPoint =
        PerCommitmentPoint <| this.RawKey().PubKey

    static member Parse (hex: string) (network: Network): PerCommitmentSecret =
        PerCommitmentSecret <| Key.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawKey().ToString network

type CommitmentSeed =
    | CommitmentSeed of PerCommitmentSecret
    with
    member this.LastPerCommitmentSecret() =
        let (CommitmentSeed lastPerCommitmentSecret) = this
        lastPerCommitmentSecret

    member this.RawKey(): Key =
        this.LastPerCommitmentSecret().RawKey()

    static member FromBytes (bytes: array<byte>): CommitmentSeed =
        CommitmentSeed <| PerCommitmentSecret.FromBytes bytes

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    static member Parse (hex: string) (network: Network): CommitmentSeed =
        CommitmentSeed <| PerCommitmentSecret.Parse hex network

    member this.ToString (network: Network) =
        this.RawKey().ToString network

/// Set of lightning keys needed to operate a channel as describe in BOLT 3
type ChannelPrivKeys = {
    FundingPrivKey: FundingPrivKey
    RevocationBasepointSecret: RevocationBasepointSecret
    PaymentBasepointSecret: PaymentBasepointSecret
    DelayedPaymentBasepointSecret: DelayedPaymentBasepointSecret
    HtlcBasepointSecret: HtlcBasepointSecret
    CommitmentSeed: CommitmentSeed
} with
    member this.ToChannelPubKeys(): ChannelPubKeys =
        {
            FundingPubKey = this.FundingPrivKey.FundingPubKey()
            RevocationBasepoint = this.RevocationBasepointSecret.RevocationBasepoint()
            PaymentBasepoint = this.PaymentBasepointSecret.PaymentBasepoint()
            DelayedPaymentBasepoint = this.DelayedPaymentBasepointSecret.DelayedPaymentBasepoint()
            HtlcBasepoint = this.HtlcBasepointSecret.HtlcBasepoint()
        }

/// This is the node-wide master key which is also used for
/// transport-level encryption. The channel's keys are derived from
/// this via BIP32 key derivation where `channelIndex` is the child
/// index used to derive the channel's master key.
type NodeMasterPrivKey =
    | NodeMasterPrivKey of ExtKey
    with
    member this.RawExtKey(): ExtKey =
        let (NodeMasterPrivKey extKey) = this
        extKey

    static member FromBytes (bytes: array<byte>): NodeMasterPrivKey =
        NodeMasterPrivKey <| ExtKey bytes

    member this.ToBytes(): array<byte> =
        this.RawExtKey().ToBytes()

    static member Parse (hex: string) (network: Network): NodeMasterPrivKey =
        NodeMasterPrivKey <| ExtKey.Parse(hex, network)

    member this.ToString (network: Network) =
        this.RawExtKey().ToString network

