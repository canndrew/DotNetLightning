namespace DotNetLightning.Crypto

open System
open NBitcoin
open NBitcoin.Crypto

open DotNetLightning.Utils

type FundingPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

type FundingKey(key: Key) =
    member this.RawKey(): Key =
        key

    member this.PubKey(): FundingPubKey =
        FundingPubKey(key.PubKey)

type RevocationBasePoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.ToBytes(): array<byte> =
        pubKey.ToBytes()

type RevocationBasePointSecret(key: Key) =
    member this.RawKey(): Key =
        key

    member this.RevocationBasePoint(): RevocationBasePoint =
        RevocationBasePoint(this.RawKey().PubKey)

type PaymentBasePoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

type DelayedPaymentBasePoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

type [<StructAttribute>] PerCommitmentSecret(key: Key) =
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
            let commonBits = thisCommitmentNumber.Index.TrailingZeros()
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

and [<StructAttribute>] PerCommitmentPoint(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    static member BytesLength: int = PubKey.BytesLength

    static member FromBytes(bytes: array<byte>): PerCommitmentPoint =
        PerCommitmentPoint <| PubKey bytes

    member this.ToBytes(): array<byte> =
        this.RawPubKey().ToBytes()

type [<StructAttribute>] CommitmentSeed(lastPerCommitmentSecret: PerCommitmentSecret) =
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

[<AutoOpen>]
module NBitcoinExtensions =
    let Secp256k1 = CryptoUtils.impl.newSecp256k1()

    type Key with
        static member FromHash(preimage: array<byte>): Key =
            Key (Hashes.SHA256 preimage)

        static member Mul(lhs: Key, rhs: Key): Key =
            let lhsBytes = lhs.ToBytes()
            let rhsBytes = rhs.ToBytes()
            let retBytes = Array.zeroCreate Key.BytesLength
            let tweak = ReadOnlySpan(lhsBytes)
            match Secp256k1.PrivateKeyTweakMultiply(tweak, rhsBytes.AsSpan()) with
            | true -> Key rhsBytes
            | false -> failwith "failed to multiply Keys"

        static member Add(lhs: Key, rhs: Key): Key =
            let lhsBytes = lhs.ToBytes()
            let rhsBytes = rhs.ToBytes()
            let tweak = ReadOnlySpan(lhsBytes)
            match Secp256k1.PrivateKeyTweakAdd(tweak, rhsBytes.AsSpan()) with
            | true -> Key rhsBytes
            | false -> failwithf "failed to add Keys"

        static member DeriveFromBasePointSecret (basePointSecret: Key)
                                                (perCommitmentPoint: PerCommitmentPoint)
                                                    : Key =
            let basePointBytes =
                let basePoint = basePointSecret.PubKey
                basePoint.ToBytes()
            let perCommitmentPointBytes = perCommitmentPoint.ToBytes()
            let tweak =
                Key.FromHash <| Array.append perCommitmentPointBytes basePointBytes
            Key.Add(basePointSecret, tweak)

    type PubKey with
        static member Mul(pubKey: PubKey, key: Key): PubKey =
            let keyBytes = key.ToBytes()
            let retBytes = Array.zeroCreate PubKey.BytesLength
            let tweak = ReadOnlySpan(keyBytes)
            match Secp256k1.PublicKeyTweakMultiply(tweak, retBytes.AsSpan()) with
            | true -> PubKey retBytes
            | false -> failwith "failed to multiplying PubKey by Key"
        
        static member Add(lhs: PubKey, rhs: PubKey): PubKey =
            let lhsBytes = lhs.ToBytes()
            let rhsBytes = rhs.ToBytes()
            match Secp256k1.PublicKeyCombine(lhsBytes.AsSpan(), rhsBytes.AsSpan()) with
            | true, result -> PubKey result
            | false, _ -> failwith "failed to add PubKeys"

        static member DeriveFromBasePoint (basePoint: PubKey)
                                          (perCommitmentPoint: PerCommitmentPoint)
                                              : PubKey =
            let basePointBytes = basePoint.ToBytes()
            let perCommitmentPointBytes = perCommitmentPoint.ToBytes()
            let tweak =
                Key.FromHash <| Array.append perCommitmentPointBytes basePointBytes
            PubKey.Add(basePoint, tweak.PubKey)


type RevocationPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    static member Derive (revocationBasePoint: RevocationBasePoint)
                         (perCommitmentPoint: PerCommitmentPoint)
                             : RevocationPubKey =
        let revocationBasePointBytes = revocationBasePoint.ToBytes()
        let perCommitmentPointBytes = perCommitmentPoint.ToBytes()
        let revocationBasePointTweak = 
            Key.FromHash <| Array.append revocationBasePointBytes perCommitmentPointBytes
        let perCommitmentPointTweak =
            Key.FromHash <| Array.append perCommitmentPointBytes revocationBasePointBytes

        RevocationPubKey <| PubKey.Add(
            PubKey.Mul(revocationBasePoint.RawPubKey(), revocationBasePointTweak),
            PubKey.Mul(perCommitmentPoint.RawPubKey(), perCommitmentPointTweak)
        )

type RevocationPrivKey(key: Key) =
    member this.RawKey(): Key =
        key

    static member Derive (revocationBasePointSecret: RevocationBasePointSecret)
                         (perCommitmentSecret: PerCommitmentSecret)
                             : RevocationPrivKey =
        let revocationBasePointBytes =
            let revocationBasePoint = revocationBasePointSecret.RevocationBasePoint()
            revocationBasePoint.ToBytes()
        let perCommitmentPointBytes =
            let perCommitmentPoint = perCommitmentSecret.PerCommitmentPoint()
            perCommitmentPoint.ToBytes()
        let revocationBasePointSecretTweak = 
            Key.FromHash <| Array.append revocationBasePointBytes perCommitmentPointBytes
        let perCommitmentSecretTweak =
            Key.FromHash <| Array.append perCommitmentPointBytes revocationBasePointBytes

        RevocationPrivKey <| Key.Add(
            Key.Mul(revocationBasePointSecret.RawKey(), revocationBasePointSecretTweak),
            Key.Mul(perCommitmentSecret.RawKey(), perCommitmentSecretTweak)
        )

type DelayedPaymentPubKey(pubKey: PubKey) =
    member this.RawPubKey(): PubKey =
        pubKey

    member this.Derive (delayedPaymentBasePoint: DelayedPaymentBasePoint)
                       (perCommitmentPoint: PerCommitmentPoint)
                           : DelayedPaymentPubKey =
        DelayedPaymentPubKey <|
            PubKey.DeriveFromBasePoint (delayedPaymentBasePoint.RawPubKey()) perCommitmentPoint

