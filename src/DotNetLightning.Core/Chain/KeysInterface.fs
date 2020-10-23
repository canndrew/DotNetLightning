namespace DotNetLightning.Chain

open System.Collections.Concurrent
open System.Text
open System.Threading
open NBitcoin
open NBitcoin.Crypto
open DotNetLightning.Utils
open DotNetLightning.Utils.NBitcoinExtensions
open DotNetLightning.Crypto

/// OutPoint
type StaticOutput = {
    outPoint: OutPoint
    output: TxOut
}

/// Outpoint commits to p2wsh
/// P2WSH should be spend by the following witness
/// `<local_delayedsig> 0 <witnessScript>` (with input nSequence set to self_delay)
/// Outputs from HTLC-Success/Timeout tx/commitment tx
type DynamicOutputP2WSH = {
    outPoint: OutPoint
    key: Key
    witnessScript: Script
    toSelfDelay: uint16
    output: TxOut
}

/// Outpoint commits to a P2WPKH
/// P2WPKH should be spend by the following witness.
/// `<local_sig> <local_pubkey>`
/// Outputs to_remote from a commitment tx
type DynamicOutputP2WPKH = {
    /// Output spendable by user wallet
    outpoint: OutPoint
    /// localkey = payment_basepoint_secret + SHA256(per_commitment_point || payment_basepoint)
    key: Key
    /// The output which is reference by the given outpoint
    output: TxOut
}

/// When on-chain outputs are created by DotNetLightning an event is generated which informs the user thereof.
/// This enum describes the format of the output and provides the OutPoint.
type SpendableOutputDescriptor =
    | StaticOutput of StaticOutput
    | DynamicOutputP2WSH of DynamicOutputP2WSH
    | DynamicOutputP2WPKH of DynamicOutputP2WPKH


/// Interface to describe an object which can get user secrets and key material.
type IKeysRepository =
    /// Secret key for node_id a.k.a network_key
    abstract member GetNodeSecret: unit -> Key
    abstract member GetDestinationScript: unit -> Script
    abstract member GetShutdownPubKey: unit -> PubKey
    /// Get a new set of ChannelKeys for per-channel secrets. These MUST be unique even if you
    /// restarted with some stale data.
    abstract member GetChannelKeys: inbound:bool -> ChannelKeys

    /// Must add funding pub key signature for to the PSBT *And* return the signature
    abstract member SignWithFundingPrivKey: psbt: PSBT -> TransactionSignature * PSBT
    /// Must add signature to the PSBT *And* return the signature
    abstract member SignHtlcTx: psbt: PSBT -> perCommitmentPoint: PerCommitmentPoint -> TransactionSignature * PSBT
    /// Must add signature to the PSBT *And* return the signature
    abstract member GenerateKeyFromRemoteSecretAndSign: psbt: PSBT * pubKey: PubKey * remoteSecret : Key -> TransactionSignature * PSBT

/// `InMemoryChannelKeys` in rust-lightning.
///
/// `nodeSecret` is the node-wide master key which is also used for
/// transport-level encryption. The channel's keys are derived from
/// `nodeSecret` via BIP32 key derivation where `channelIndex` is the child
/// index used to derive the channel's master key.
type DefaultKeyRepository(nodeSecret: ExtKey, channelIndex: int) =
    let _utf8 = Encoding.UTF8
    let channelMasterKey = nodeSecret.Derive(channelIndex, true)

    let destinationKey = channelMasterKey.Derive(1, true).PrivateKey
    let shutdownKey = channelMasterKey.Derive(2, true).PrivateKey
    let commitmentSeed = channelMasterKey.Derive(3, true).PrivateKey |> CommitmentSeed

    let fundingPrivKey =
        channelMasterKey.Derive(4, true).PrivateKey |> FundingPrivKey
    let fundingPubKey = fundingPrivKey.FundingPubKey()

    let revocationBasepointSecret =
        channelMasterKey.Derive(5, true).PrivateKey |> RevocationBasepointSecret
    let revocationBasepoint = revocationBasepointSecret.RevocationBasepoint()

    let paymentBasepointSecret =
        channelMasterKey.Derive(6, true).PrivateKey |> PaymentBasepointSecret
    let paymentBasepoint = paymentBasepointSecret.PaymentBasepoint()

    let delayedPaymentBasepointSecret =
        channelMasterKey.Derive(7, true).PrivateKey |> DelayedPaymentBasepointSecret
    let delayedPaymentBasepoint = delayedPaymentBasepointSecret.DelayedPaymentBasepoint()

    let htlcBasepointSecret =
        channelMasterKey.Derive(8, true).PrivateKey |> HtlcBasepointSecret
    let htlcBasepoint = htlcBasepointSecret.HtlcBasepoint()

    let basepointToSecretMap = ConcurrentDictionary<PubKey, Key>()
    do
        basepointToSecretMap.TryAdd(fundingPubKey.RawPubKey(), fundingPrivKey.RawKey()) |> ignore
        basepointToSecretMap.TryAdd(revocationBasepoint.RawPubKey(), revocationBasepointSecret.RawKey()) |> ignore
        basepointToSecretMap.TryAdd(paymentBasepoint.RawPubKey(), paymentBasepointSecret.RawKey()) |> ignore
        basepointToSecretMap.TryAdd(delayedPaymentBasepoint.RawPubKey(), delayedPaymentBasepointSecret.RawKey()) |> ignore
        basepointToSecretMap.TryAdd(htlcBasepoint.RawPubKey(), htlcBasepointSecret.RawKey()) |> ignore

    member this.NodeSecret = nodeSecret
    member this.DestinationScript = destinationKey.PubKey.WitHash.ScriptPubKey
    member this.ShutDownKey = shutdownKey
    member this.ShutDownPubKey = shutdownKey.PubKey
    member this.CommitmentSeed = commitmentSeed
    member this.FundingPrivKey = fundingPrivKey
    member this.FundingPubKey = fundingPubKey
    member this.RevocationBasepointSecret = revocationBasepointSecret
    member this.RevocationBasepoint = revocationBasepoint
    member this.PaymentBasepointSecret = paymentBasepointSecret
    member this.PaymentBasepoint = paymentBasepoint
    member this.DelayedPaymentBasepointSecret = delayedPaymentBasepointSecret
    member this.DelayedPaymentBasepoint = delayedPaymentBasepoint
    member this.HtlcBasepointSecret = htlcBasepointSecret
    member this.HtlcBasepoint = htlcBasepoint

    member val BasepointToSecretMap = basepointToSecretMap

    member this.GetChannelKeys(): ChannelKeys =
        {
            FundingPrivKey = this.FundingPrivKey
            RevocationBasepointSecret = this.RevocationBasepointSecret
            PaymentBasepointSecret = this.PaymentBasepointSecret
            DelayedPaymentBasepointSecret = this.DelayedPaymentBasepointSecret
            HtlcBasepointSecret = this.HtlcBasepointSecret
            CommitmentSeed = this.CommitmentSeed
        }
    interface IKeysRepository with
        // TODO: Update
        member this.GetChannelKeys(_inbound): ChannelKeys =
            this.GetChannelKeys()
        member this.GetDestinationScript() =
            this.DestinationScript
        member this.GetShutdownPubKey(): PubKey =
            this.ShutDownPubKey
        member this.GetNodeSecret() =
            this.NodeSecret.PrivateKey

        member this.SignWithFundingPrivKey (psbt: PSBT) =
            psbt.SignWithKeys(this.FundingPrivKey.RawKey()) |> ignore
            match psbt.GetMatchingSig(this.FundingPubKey.RawPubKey()) with
            | Some signature -> (signature, psbt)
            | None -> failwithf "Failed to get signature for %A. by funding pub key (%A). This should never happen" psbt this.FundingPubKey

        member this.SignHtlcTx (psbt: PSBT)
                               (perCommitmentPoint: PerCommitmentPoint)
                                   : TransactionSignature * PSBT =
            let htlcPrivKey = perCommitmentPoint.DeriveHtlcPrivKey this.HtlcBasepointSecret
            let htlcPubKey = htlcPrivKey.HtlcPubKey()
            psbt.SignWithKeys(htlcPrivKey.RawKey()) |> ignore
            match psbt.GetMatchingSig(htlcPubKey.RawPubKey()) with
            | Some signature -> (signature, psbt)
            | None ->
                failwithf
                    "failed to get htlc signature for %A.\n htlc pubkey was: (%A).\n and perCommitmentPoint was (%A)"
                    psbt htlcPubKey perCommitmentPoint

        member this.GenerateKeyFromRemoteSecretAndSign(_psbt, _pubkey, _remoteSecret) =
            failwith "Not implemented: DefaultKeyRepository::GenerateKeyFromRemoteSecretAndSign"
