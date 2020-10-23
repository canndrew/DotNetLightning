namespace DotNetLightning.Crypto

open System
open NBitcoin
open NBitcoin.Crypto

open DotNetLightning.Utils
open DotNetLightning.Core.Utils.Extensions

// FIXME: Should the [<Struct>]-annotated types here be changed to records or single-constructor discriminated unions? 
    
type [<Struct>] RevocationBasepoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        pubKey.ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] RevocationBasepointSecret(key: Key) =
    member this.RawKey(): Key =
        key

    member this.RevocationBasepoint(): RevocationBasepoint =
        RevocationBasepoint(this.RawKey().PubKey)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

type [<Struct>] RevocationPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        pubKey.ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] RevocationPrivKey(key: Key) =
    member this.RawKey(): Key =
        key

    member this.ToBytes(): array<byte> =
        key.ToBytes()

type [<Struct>] PaymentBasepoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        pubKey.ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] PaymentBasepointSecret(key: Key) =
    member this.RawKey(): Key =
        key

    member this.PaymentBasepoint(): PaymentBasepoint =
        PaymentBasepoint(this.RawKey().PubKey)

type [<Struct>] PaymentPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] PaymentPrivKey(key: Key) =
    member this.RawKey(): Key =
        key

    member this.PaymentPubKey(): PaymentPubKey =
        PaymentPubKey <| this.RawKey().PubKey

type [<Struct>] DelayedPaymentBasepoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] DelayedPaymentBasepointSecret(key: Key) =
    member this.RawKey(): Key =
        key

    member this.DelayedPaymentBasepoint(): DelayedPaymentBasepoint =
        DelayedPaymentBasepoint(this.RawKey().PubKey)

type [<Struct>] DelayedPaymentPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] DelayedPaymentPrivKey(key: Key) =
    member this.RawKey(): Key =
        key

    member this.DelayedPaymentPubKey(): DelayedPaymentPubKey =
        DelayedPaymentPubKey <| this.RawKey().PubKey

type [<Struct>] HtlcBasepoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] HtlcBasepointSecret(key: Key) =
    member this.RawKey(): Key =
        key

    member this.HtlcBasepoint(): HtlcBasepoint =
        HtlcBasepoint(this.RawKey().PubKey)

type [<Struct>] HtlcPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

    override self.ToString() =
        pubKey.ToString()

type [<Struct>] HtlcPrivKey(key: Key) =
    member this.RawKey(): Key =
        key

    member this.HtlcPubKey(): HtlcPubKey =
        HtlcPubKey <| this.RawKey().PubKey

/// Set of lightning keys needed to operate a channel as describe in BOLT 3
type ChannelKeys = {
    FundingKey: Key
    RevocationBasepointSecret: RevocationBasepointSecret
    PaymentBasepointSecret: PaymentBasepointSecret
    DelayedPaymentBasepointSecret: DelayedPaymentBasepointSecret
    HtlcBasepointSecret: HtlcBasepointSecret
    CommitmentSeed: CommitmentSeed
} with
    member this.ToChannelPubKeys(): ChannelPubKeys =
        {
            FundingPubKey = this.FundingKey.PubKey
            RevocationBasepoint = this.RevocationBasepointSecret.RevocationBasepoint()
            PaymentBasepoint = this.PaymentBasepointSecret.PaymentBasepoint()
            DelayedPaymentBasepoint = this.DelayedPaymentBasepointSecret.DelayedPaymentBasepoint()
            HtlcBasepoint = this.HtlcBasepointSecret.HtlcBasepoint()
        }

/// In usual operation we should not hold secrets on memory. So only hold pubkey
and ChannelPubKeys = {
    FundingPubKey: PubKey
    RevocationBasepoint: RevocationBasepoint
    PaymentBasepoint: PaymentBasepoint
    DelayedPaymentBasepoint: DelayedPaymentBasepoint
    HtlcBasepoint: HtlcBasepoint
}

and [<Struct>] CommitmentNumber(index: UInt48) =
    member this.Index = index

    override this.ToString() =
        sprintf "%012x (#%i)" this.Index.UInt64 (UInt48.MaxValue - this.Index).UInt64

    static member LastCommitment: CommitmentNumber =
        CommitmentNumber UInt48.Zero

    static member FirstCommitment: CommitmentNumber =
        CommitmentNumber UInt48.MaxValue

    static member ObscureFactor (isFunder: bool)
                                (localPaymentBasepoint: PaymentBasepoint)
                                (remotePaymentBasepoint: PaymentBasepoint)
                                    : UInt48 =
        let pubKeysHash =
            if isFunder then
                let ba =
                    Array.concat
                        (seq [ yield localPaymentBasepoint.ToBytes(); yield remotePaymentBasepoint.ToBytes() ])
                Hashes.SHA256 ba
            else
                let ba =
                    Array.concat
                        (seq [ yield remotePaymentBasepoint.ToBytes(); yield localPaymentBasepoint.ToBytes() ])
                Hashes.SHA256 ba
        UInt48.FromBytesBigEndian pubKeysHash.[26..]

    member this.PreviousCommitment: CommitmentNumber =
        CommitmentNumber(this.Index + UInt48.One)

    member this.NextCommitment: CommitmentNumber =
        CommitmentNumber(this.Index - UInt48.One)

    member this.Subsumes(other: CommitmentNumber): bool =
        let trailingZeros = this.Index.TrailingZeros
        (this.Index >>> trailingZeros) = (other.Index >>> trailingZeros)

    member this.PreviousUnsubsumed: Option<CommitmentNumber> =
        let trailingZeros = this.Index.TrailingZeros
        let prev = this.Index.UInt64 + (1UL <<< trailingZeros)
        if prev > UInt48.MaxValue.UInt64 then
            None
        else
            Some <| CommitmentNumber(UInt48.FromUInt64 prev)

    member this.Obscure (isFunder: bool)
                        (localPaymentBasepoint: PaymentBasepoint)
                        (remotePaymentBasepoint: PaymentBasepoint)
                            : ObscuredCommitmentNumber =
        let obscureFactor =
            CommitmentNumber.ObscureFactor
                isFunder
                localPaymentBasepoint
                remotePaymentBasepoint
        ObscuredCommitmentNumber((UInt48.MaxValue - this.Index) ^^^ obscureFactor)

and [<Struct>] ObscuredCommitmentNumber(obscuredIndex: UInt48) =
    member this.ObscuredIndex: UInt48 = obscuredIndex

    override this.ToString() =
        sprintf "%012x" this.ObscuredIndex.UInt64

    member this.LockTime: LockTime =
        Array.concat [| [| 0x20uy |]; this.ObscuredIndex.GetBytesBigEndian().[3..] |]
        |> System.UInt32.FromBytesBigEndian
        |> LockTime

    member this.Sequence: Sequence =
        Array.concat [| [| 0x80uy |]; this.ObscuredIndex.GetBytesBigEndian().[..2] |]
        |> System.UInt32.FromBytesBigEndian
        |> Sequence

    static member TryFromLockTimeAndSequence (lockTime: LockTime)
                                             (sequence: Sequence)
                                                 : Option<ObscuredCommitmentNumber> =
        let lockTimeBytes = lockTime.Value.GetBytesBigEndian()
        let sequenceBytes = sequence.Value.GetBytesBigEndian()
        if lockTimeBytes.[0] <> 0x20uy || sequenceBytes.[0] <> 0x80uy then
            None
        else
            Array.concat [| sequenceBytes.[1..]; lockTimeBytes.[1..] |]
            |> UInt48.FromBytesBigEndian
            |> ObscuredCommitmentNumber
            |> Some

    member this.Unobscure (isFunder: bool)
                          (localPaymentBasepoint: PaymentBasepoint)
                          (remotePaymentBasepoint: PaymentBasepoint)
                              : CommitmentNumber =
        let obscureFactor =
            CommitmentNumber.ObscureFactor
                isFunder
                localPaymentBasepoint
                remotePaymentBasepoint
        CommitmentNumber(UInt48.MaxValue - (this.ObscuredIndex ^^^ obscureFactor))

and [<Struct>] PerCommitmentSecret(key: Key) =
    member this.RawKey(): Key =
        key

    static member BytesLength: int = Key.BytesLength

    static member FromBytes(bytes: array<byte>): PerCommitmentSecret =
        PerCommitmentSecret <| new Key(bytes)

    member this.ToBytes(): array<byte> =
        this.RawKey().ToBytes()

    member this.DeriveChild (thisCommitmentNumber: CommitmentNumber)
                            (childCommitmentNumber: CommitmentNumber)
                                : Option<PerCommitmentSecret> =
        if thisCommitmentNumber.Subsumes childCommitmentNumber then
            let commonBits = thisCommitmentNumber.Index.TrailingZeros
            let index = childCommitmentNumber.Index
            let mutable secret = this.ToBytes()
            for bit in (commonBits - 1) .. -1 .. 0 do
                if (index >>> bit) &&& UInt48.One = UInt48.One then
                    let byteIndex = bit / 8
                    let bitIndex = bit % 8
                    secret.[byteIndex] <- secret.[byteIndex] ^^^ (1uy <<< bitIndex)
                    secret <- Hashes.SHA256 secret
            Some <| PerCommitmentSecret(new Key(secret))
        else
            None

    member this.PerCommitmentPoint(): PerCommitmentPoint =
        PerCommitmentPoint <| this.RawKey().PubKey

and [<Struct>] PerCommitmentPoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    static member BytesLength: int = PubKey.BytesLength

    static member FromBytes(bytes: array<byte>): PerCommitmentPoint =
        PerCommitmentPoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

and [<Struct>] CommitmentSeed(lastPerCommitmentSecret: PerCommitmentSecret) =
    new(key: Key) =
        CommitmentSeed(PerCommitmentSecret key)

    member this.LastPerCommitmentSecret = lastPerCommitmentSecret

    member this.DerivePerCommitmentSecret (commitmentNumber: CommitmentNumber): PerCommitmentSecret =
        let res =
            this.LastPerCommitmentSecret.DeriveChild
                CommitmentNumber.LastCommitment
                commitmentNumber
        match res with
        | Some perCommitmentSecret -> perCommitmentSecret
        | None ->
            failwith
                "The final per commitment secret should be able to derive the \
                commitment secret for all prior commitments. This is a bug."

    member this.DerivePerCommitmentPoint (commitmentNumber: CommitmentNumber): PerCommitmentPoint =
        let perCommitmentSecret = this.DerivePerCommitmentSecret commitmentNumber
        perCommitmentSecret.PerCommitmentPoint()

