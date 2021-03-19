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
let testList = [
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

        let fundingScriptPubKey =
            Scripts.funding
                (state.InputInitFunder.ChannelPrivKeys.ToChannelPubKeys().FundingPubKey)
                msg.FundingPubKey
        let fundingDestination = fundingScriptPubKey.WitHash :> IDestination
        let fundingAmount = state.InputInitFunder.FundingSatoshis
        let fundingScriptCoin = ScriptCoin(Coin(randomOutpoint, 

        let _commitments =
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
                DustLimitSatoshis = 546 |> Money.Satoshis
                MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
                ChannelReserveSatoshis = 1000L |> Money.Satoshis
                HTLCMinimumMSat = 1000L |> LNMoney
                ToSelfDelay = 144us |> BlockHeightOffset16
                MaxAcceptedHTLCs = 1000us
                ChannelPubKeys = remoteChannelPubKeys
                Features = FeatureBits.Zero
                MinimumDepth = 6us |> BlockHeightOffset32
            }
            {
                LocalParams = localParams
                RemoteParams = remoteParams
                ChannelFlags = 0
                FundingScriptCoin = 
            }
        ()

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
]
