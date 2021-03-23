module TransactionTests

open System
open ResultUtils

open DotNetLightning.Transactions
open DotNetLightning.Transactions.Transactions
open DotNetLightning.Utils
open DotNetLightning.Crypto
open DotNetLightning.Channel
open DotNetLightning.Serialization
open Expecto
open NBitcoin

let n = Network.RegTest

[<Tests>]
let testList = testList "transaction tests" [
    testCase "check tryGetFundsFromLocalCommitmentTx" <| fun _ ->
        let rand = new Random()

        let localNodeMasterPrivKey =
            let extKey = new ExtKey()
            NodeMasterPrivKey extKey
        let localNodeSecret = localNodeMasterPrivKey.NodeSecret()
        let localNodeId = localNodeSecret.NodeId()
        let localChannelPrivKeys = localNodeMasterPrivKey.ChannelPrivKeys (rand.Next(1, 100))
        let localChannelPubKeys = localChannelPrivKeys.ToChannelPubKeys()
        let localDestPrivKey = new Key()
        let localDestPubKey = localDestPrivKey.PubKey

        let remoteNodeMasterPrivKey =
            let extKey = new ExtKey()
            NodeMasterPrivKey extKey
        let remoteNodeSecret = remoteNodeMasterPrivKey.NodeSecret()
        let remoteNodeId = remoteNodeSecret.NodeId()
        let remoteChannelPrivKeys = remoteNodeMasterPrivKey.ChannelPrivKeys (rand.Next(1, 100))
        let remoteChannelPubKeys = remoteChannelPrivKeys.ToChannelPubKeys()
        let remoteDestPrivKey = new Key()
        let remoteDestPubKey = remoteDestPrivKey.PubKey

        let randomU256(): uint256 =
            let bytes = Array.ofSeq <| seq {
                for i in 0 .. 31 do
                    yield byte (rand.Next(1, 256))
            }
            uint256 bytes

        let fundingAmount = 10_000_000L |> Money.Satoshis
        let fundingScriptPubKey =
            Scripts.funding
                localChannelPubKeys.FundingPubKey
                remoteChannelPubKeys.FundingPubKey
        let fundingDestination = fundingScriptPubKey.WitHash :> IDestination
        let fundingCoin = Coin(randomU256(), uint32(rand.Next(0, 10)), fundingAmount, fundingDestination.ScriptPubKey)
        let fundingScriptCoin = ScriptCoin(fundingCoin, fundingScriptPubKey)

        let commitmentNumber = rand.Next(1, 100) |> uint64 |> UInt48.FromUInt64 |> CommitmentNumber
        let perCommitmentSecret = localChannelPrivKeys.CommitmentSeed.DerivePerCommitmentSecret commitmentNumber
        let perCommitmentPoint = perCommitmentSecret.PerCommitmentPoint()
        let localCommitmentPubKeys = perCommitmentPoint.DeriveCommitmentPubKeys localChannelPubKeys
        let remoteCommitmentPubKeys = perCommitmentPoint.DeriveCommitmentPubKeys remoteChannelPubKeys

        let localParams = {
            NodeId = localNodeId
            ChannelPubKeys = localChannelPubKeys
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = 1000L |> LNMoney
            ToSelfDelay = 144us |> BlockHeightOffset16
            MaxAcceptedHTLCs = 1000us
            IsFunder = true
            DefaultFinalScriptPubKey = localDestPubKey.ScriptPubKey
            Features = FeatureBits.Zero
        }
        let remoteParams = {
            NodeId = remoteNodeId
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = 1000L |> LNMoney
            ToSelfDelay = 144us |> BlockHeightOffset16
            MaxAcceptedHTLCs = 1000us
            ChannelPubKeys = remoteChannelPubKeys
            Features = FeatureBits.Zero
            MinimumDepth = 6u |> BlockHeightOffset32
        }
        let localDustLimit = 546L |> Money.Satoshis
        let toLocalDelay = 144us |> BlockHeightOffset16
        let commitmentSpec = {
            HTLCs = Map.empty
            FeeRatePerKw = FeeRatePerKw 123u
            ToLocal = 2_000_000_000L |> LNMoney
            ToRemote = 8_000_000_000L |> LNMoney
        }

        let unsignedCommitmentTx =
            makeCommitTx
                fundingScriptCoin
                commitmentNumber
                localChannelPubKeys.PaymentBasepoint
                remoteChannelPubKeys.PaymentBasepoint
                true
                localParams.DustLimitSatoshis
                localCommitmentPubKeys.RevocationPubKey
                localParams.ToSelfDelay
                localCommitmentPubKeys.DelayedPaymentPubKey
                remoteCommitmentPubKeys.PaymentPubKey
                localCommitmentPubKeys.HtlcPubKey
                remoteCommitmentPubKeys.HtlcPubKey
                commitmentSpec
                Network.RegTest
        let commitmentTx =
            let thing = unsignedCommitmentTx.Value
            (*
            unsignedCommitmentTx.Value
                .SignWithKeys(localChannelPrivKeys.FundingPrivKey.RawKey(), remoteChannelPrivKeys.FundingPrivKey.RawKey())
                .ExtractTransaction()
            *)
            thing.SignWithKeys(localChannelPrivKeys.FundingPrivKey.RawKey(), remoteChannelPrivKeys.FundingPrivKey.RawKey())
            thing.ExtractTransaction()

        let transactionBuilder =
            ForceCloseFundsRecovery.tryGetFundsFromLocalCommitmentTx
                localParams
                remoteParams
                fundingScriptCoin
                localChannelPrivKeys
                Network.RegTest
                commitmentTx
        Expect.equal(0, 1)
        ()

    (*
    testCase "check pre-computed transaction weights" <| fun _ ->
        let localRevocationPriv = [| for _ in 0..31 -> 0xccuy |] |> fun b -> new Key(b)
        let localPaymentPriv = [| for _ in 0..31 -> 0xdduy |] |> fun b -> new Key(b)
        let remotePaymentPriv = [| for _ in 0..31 -> 0xeeuy |] |> fun b -> new Key(b)
        let localHtlcPriv = [| for _ in 0..31 -> 0xeauy |] |> fun b -> new Key(b)
        let remoteHtlcPriv = [| for _ in 0..31 -> 0xebuy |] |> fun b -> new Key(b)
        let localFinalPriv = [| for _ in 0..31 -> 0xffuy |] |> fun b -> new Key(b)
        let finalSpk =
            let s = [| for _ in 0..31 -> 0xfeuy |] |> fun b -> new Key(b)
            s.PubKey.WitHash
        let localDustLimit = 546L |> Money.Satoshis
        let toLocalDelay= 144us |> BlockHeightOffset16
        let feeRatePerKw = 1000u |> FeeRatePerKw
        
        let _ =
            let pubkeyScript = localPaymentPriv.PubKey.WitHash.ScriptPubKey
            let commitTx =
                let t = n.CreateTransaction()
                t.Version <- 0u
                t.Outputs.Add(TxOut(Money.Satoshis(20000L), pubkeyScript)) |> ignore
                t.LockTime <- LockTime.Zero
                t
            let claimP2WPKHOutputTx =
                Transactions.makeClaimP2WPKHOutputTx(commitTx)
                                                    (localDustLimit)
                                                    (PaymentPubKey localPaymentPriv.PubKey)
                                                    (finalSpk)
                                                    (feeRatePerKw)
                                                    n |> Result.defaultWith (fun _  -> failwith "fail: precomputed tx weights")
            let weight =
                let tx = claimP2WPKHOutputTx.Value.GetGlobalTransaction()
                let witScript =
                    let dummySig = [| for _ in 0..70 -> 0xbbuy |]
                    let dummyPk = (new Key()).PubKey.ToBytes()
                    let dummy = seq[ Op.GetPushOp(dummySig); Op.GetPushOp(dummyPk)]
                    Script(dummy).ToWitScript()
                tx.Inputs.[0].WitScript <- witScript
                tx.GetVirtualSize() |> uint64
            Expect.equal(Constants.CLAIM_P2WPKH_OUTPUT_WEIGHT) (weight) ""
            ()
            
        ()
    *)
]
