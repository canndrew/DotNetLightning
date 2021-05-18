module TransactionTests

open System
open ResultUtils
open ResultUtils.Portability

open DotNetLightning.Transactions
open DotNetLightning.Transactions.Transactions
open DotNetLightning.Utils
open DotNetLightning.Crypto
open DotNetLightning.Channel
open DotNetLightning.Serialization
open Expecto
open NBitcoin
open NBitcoin.DataEncoders

let n = Network.RegTest

[<Tests>]
let testList = testList "transaction tests" [
    (*
    testCase "check fund recovery from local/remote commitment txs" <| fun _ ->
        let rand = Random()

        let localNodeMasterPrivKey =
            let extKey = ExtKey()
            NodeMasterPrivKey extKey
        let localNodeSecret = localNodeMasterPrivKey.NodeSecret()
        let localNodeId = localNodeSecret.NodeId()
        let localChannelPrivKeys = localNodeMasterPrivKey.ChannelPrivKeys (rand.Next(1, 100))
        let localChannelPubKeys = localChannelPrivKeys.ToChannelPubKeys()
        let localDestPrivKey = new Key()
        let localDestPubKey = localDestPrivKey.PubKey

        let remoteNodeMasterPrivKey =
            let extKey = ExtKey()
            NodeMasterPrivKey extKey
        let remoteNodeSecret = remoteNodeMasterPrivKey.NodeSecret()
        let remoteNodeId = remoteNodeSecret.NodeId()
        let remoteChannelPrivKeys = remoteNodeMasterPrivKey.ChannelPrivKeys (rand.Next(1, 100))
        let remoteChannelPubKeys = remoteChannelPrivKeys.ToChannelPubKeys()

        let fundingAmount = 10_000_000L |> Money.Satoshis
        let fundingScriptPubKey =
            Scripts.funding
                localChannelPubKeys.FundingPubKey
                remoteChannelPubKeys.FundingPubKey
        let fundingDestination = fundingScriptPubKey.WitHash :> IDestination
        let fundingTxId = NBitcoin.RandomUtils.GetUInt256()
        let fundingOutputIndex = uint32(rand.Next(0, 10))
        let fundingCoin = Coin(fundingTxId, fundingOutputIndex, fundingAmount, fundingDestination.ScriptPubKey)
        let fundingScriptCoin = ScriptCoin(fundingCoin, fundingScriptPubKey)

        let commitmentNumber =
            let uint48 = rand.Next(1, 100) |> uint64 |> UInt48.FromUInt64
            CommitmentNumber (UInt48.MaxValue - uint48)
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
        let feeRate = FeeRatePerKw (rand.Next(0, 300) |> uint32)
        let localAmount = 2_000_000_000L |> LNMoney
        let remoteAmount = LNMoney.Satoshis(fundingAmount.Satoshi) - localAmount
        let commitmentSpec = {
            HTLCs = Map.empty
            FeeRatePerKw = feeRate
            ToLocal = localAmount
            ToRemote = remoteAmount
        }

        let unsignedCommitmentTx =
            makeCommitTx
                fundingScriptCoin
                commitmentNumber
                localChannelPubKeys.PaymentBasepoint
                remoteChannelPubKeys.PaymentBasepoint
                true
                localParams.DustLimitSatoshis
                remoteCommitmentPubKeys.RevocationPubKey
                localParams.ToSelfDelay
                localCommitmentPubKeys.DelayedPaymentPubKey
                remoteCommitmentPubKeys.PaymentPubKey
                localCommitmentPubKeys.HtlcPubKey
                remoteCommitmentPubKeys.HtlcPubKey
                commitmentSpec
                Network.RegTest
        let commitmentTx =
            unsignedCommitmentTx.Value
                .SignWithKeys(localChannelPrivKeys.FundingPrivKey.RawKey(), remoteChannelPrivKeys.FundingPrivKey.RawKey())
                .Finalize()
                .ExtractTransaction()

        let transactionBuilder =
            ForceCloseFundsRecovery.tryGetFundsFromLocalCommitmentTx
                localParams
                remoteParams
                fundingScriptCoin
                localChannelPrivKeys
                Network.RegTest
                commitmentTx
            |> Result.deref

        let recoveryTransaction =
            transactionBuilder
                .SendAll(localDestPubKey)
                .BuildTransaction(true)
        let inputs = recoveryTransaction.Inputs
        Expect.equal inputs.Count 1 "wrong number of inputs"
        let input = inputs.[0]
        Expect.equal input.Sequence.Value (uint32 localParams.ToSelfDelay.Value) "wrong sequence nuber"
        Expect.equal input.PrevOut.Hash (commitmentTx.GetHash()) "wrong prevout hash"
        Expect.equal input.ScriptSig Script.Empty 
        Console.WriteLine(sprintf "%A" input.WitScript)
        //Expect.equal input.WitScript.PushCount 3
        if input.WitScript = WitScript.Empty then
            Console.WriteLine(sprintf "localParams == %A" localParams)
            Console.WriteLine(sprintf "localParams.Features == %A" localParams.Features.ByteArray)
            Console.WriteLine(sprintf "remoteParams == %A" remoteParams)
            Console.WriteLine(sprintf "remoteParams.Features == %A" remoteParams.Features.ByteArray)
            Console.WriteLine(sprintf "fundingScriptCoin == %A" fundingScriptCoin)
            Console.WriteLine(sprintf "fundingScriptCoin.Outpoint == %A" fundingScriptCoin.Outpoint)
            Console.WriteLine(sprintf "fundingScriptCoin.TxOut.ScriptPubKey == %A" fundingScriptCoin.TxOut.ScriptPubKey)
            Console.WriteLine(sprintf "fundingScriptCoin.TxOut.Value == %A" fundingScriptCoin.TxOut.Value)
            Console.WriteLine(sprintf "fundingScriptCoin.Amount == %A" fundingScriptCoin.Amount)
            Console.WriteLine(sprintf "fundingScriptCoin.Redeem == %A" fundingScriptCoin.Redeem)
            Console.WriteLine(sprintf "localChannelPrivKeys == %A" localChannelPrivKeys)
            Console.WriteLine(sprintf "localChannelPrivKeys.FundingPrivKey == %A" (localChannelPrivKeys.FundingPrivKey.RawKey().ToHex()))
            Console.WriteLine(sprintf "localChannelPrivKeys.RevocationBasepointSecret == %A" (localChannelPrivKeys.RevocationBasepointSecret.RawKey().ToHex()))
            Console.WriteLine(sprintf "localChannelPrivKeys.PaymentBasepointSecret == %A" (localChannelPrivKeys.PaymentBasepointSecret.RawKey().ToHex()))
            Console.WriteLine(sprintf "localChannelPrivKeys.DelayedPaymentBasepointSecret == %A" (localChannelPrivKeys.DelayedPaymentBasepointSecret.RawKey().ToHex()))
            Console.WriteLine(sprintf "localChannelPrivKeys.HtlcBasepointSecret == %A" (localChannelPrivKeys.HtlcBasepointSecret.RawKey().ToHex()))
            Console.WriteLine(sprintf "localChannelPrivKeys.CommitmentSeed == %A" (localChannelPrivKeys.CommitmentSeed.LastPerCommitmentSecret().RawKey().ToHex()))
            Console.WriteLine(sprintf "commitmentTx == %s" (commitmentTx.ToHex()))
            Console.WriteLine(sprintf "recoveryTransaction == %A" recoveryTransaction)
            Console.WriteLine(sprintf "localDestPubKey == %A" localDestPubKey)
            failwith "empty witness for local recovery tx"

        let expectedAmount =
            let fullAmount = commitmentSpec.ToLocal.ToMoney()
            let fee = commitmentTx.GetFee [| fundingScriptCoin |]
            fullAmount - fee
        let actualAmount =
            commitmentTx.Outputs.[input.PrevOut.N].Value
        Expect.equal actualAmount expectedAmount "wrong prevout amount"

        let remoteDestPrivKey = new Key()
        let remoteDestPubKey = remoteDestPrivKey.PubKey
        let remoteRemotePerCommitmentSecrets =
            let rec addKeys (remoteRemotePerCommitmentSecrets: PerCommitmentSecretStore)
                            (currentCommitmentNumber: CommitmentNumber)
                                : PerCommitmentSecretStore =
                if currentCommitmentNumber = commitmentNumber then
                    remoteRemotePerCommitmentSecrets
                else
                    let currentPerCommitmentSecret =
                        localChannelPrivKeys.CommitmentSeed.DerivePerCommitmentSecret
                            currentCommitmentNumber
                    let nextLocalPerCommitmentSecretsRes =
                        remoteRemotePerCommitmentSecrets.InsertPerCommitmentSecret
                            currentCommitmentNumber
                            currentPerCommitmentSecret
                    addKeys
                        (Result.deref nextLocalPerCommitmentSecretsRes)
                        (currentCommitmentNumber.NextCommitment())
            addKeys (PerCommitmentSecretStore()) CommitmentNumber.FirstCommitment

        let remoteRemoteCommit = {
            Index = commitmentNumber
            Spec = commitmentSpec
            TxId = TxId <| commitmentTx.GetHash()
            RemotePerCommitmentPoint = perCommitmentPoint
        }
        let remoteLocalParams: LocalParams = {
            NodeId = remoteParams.NodeId
            ChannelPubKeys = remoteParams.ChannelPubKeys
            DustLimitSatoshis = remoteParams.DustLimitSatoshis
            MaxHTLCValueInFlightMSat = remoteParams.MaxHTLCValueInFlightMSat
            ChannelReserveSatoshis = remoteParams.ChannelReserveSatoshis
            HTLCMinimumMSat = remoteParams.HTLCMinimumMSat
            ToSelfDelay = remoteParams.ToSelfDelay
            MaxAcceptedHTLCs = remoteParams.MaxAcceptedHTLCs
            IsFunder = false
            DefaultFinalScriptPubKey = remoteDestPubKey.ScriptPubKey
            Features = remoteParams.Features
        }
        let remoteRemoteParams = {
            NodeId = localParams.NodeId
            DustLimitSatoshis = localParams.DustLimitSatoshis
            MaxHTLCValueInFlightMSat = localParams.MaxHTLCValueInFlightMSat
            ChannelReserveSatoshis = localParams.ChannelReserveSatoshis
            HTLCMinimumMSat = localParams.HTLCMinimumMSat
            ToSelfDelay = localParams.ToSelfDelay
            MaxAcceptedHTLCs = localParams.MaxAcceptedHTLCs
            ChannelPubKeys = localParams.ChannelPubKeys
            Features = localParams.Features
            MinimumDepth = 6u |> BlockHeightOffset32
        }

        let transactionBuilder =
            ForceCloseFundsRecovery.tryGetFundsFromRemoteCommitmentTx
                remoteLocalParams
                remoteRemoteParams
                fundingScriptCoin
                remoteRemotePerCommitmentSecrets
                remoteRemoteCommit
                remoteChannelPrivKeys
                Network.RegTest
                commitmentTx
            |> Result.deref

        let recoveryTransaction =
            transactionBuilder
                .SendAll(remoteDestPubKey)
                .BuildTransaction(true)
        let inputs = recoveryTransaction.Inputs
        Expect.equal inputs.Count 1 "wrong number of inputs"
        let input = inputs.[0]
        Expect.equal input.PrevOut.Hash (commitmentTx.GetHash()) "wrong prevout hash"
        let expectedAmount = commitmentSpec.ToRemote.ToMoney()
        let actualAmount =
            commitmentTx.Outputs.[input.PrevOut.N].Value
        Expect.equal actualAmount expectedAmount "wrong prevout amount"

        let transactionBuilder =
            ForceCloseFundsRecovery.createPenaltyTx
                remoteLocalParams
                remoteRemoteParams
                perCommitmentSecret
                remoteRemoteCommit
                remoteChannelPrivKeys
                Network.RegTest
        let penaltyTransaction =
            transactionBuilder
                .SendAll(remoteDestPubKey)
                .BuildTransaction(true)
        let inputs = penaltyTransaction.Inputs
        Expect.equal inputs.Count 2 "wrong number of inputs"
        Expect.equal inputs.[0].PrevOut.Hash (commitmentTx.GetHash()) "wrong prevout hash on input 0"
        Expect.equal inputs.[1].PrevOut.Hash (commitmentTx.GetHash()) "wrong prevout hash on input 1"

        let expectedAmountFromToLocal =
            let localAmount = commitmentSpec.ToLocal.ToMoney()
            let fee = commitmentTx.GetFee [| fundingScriptCoin |]
            localAmount - fee
        let expectedAmountFromToRemote =
            commitmentSpec.ToRemote.ToMoney()

        let actualAmount0 =
            commitmentTx.Outputs.[inputs.[0].PrevOut.N].Value
        let actualAmount1 =
            commitmentTx.Outputs.[inputs.[1].PrevOut.N].Value

        if actualAmount0 = expectedAmountFromToLocal then
            Expect.equal actualAmount1 expectedAmountFromToRemote "wrong prevout amount for to_remote"
        elif actualAmount0 = expectedAmountFromToRemote then
            Expect.equal actualAmount1 expectedAmountFromToLocal "wrong prevout amount for to_local"
        else
            failwith "amount of input 0 does not match either expected amount"
    *)

    (*
    testCase "check non-empty witness for recovery tx" <| fun _ ->
        let key(hex: string): Key =
            new Key(Encoders.Hex.DecodeData(hex))
        let localParams = {
            NodeId = NodeId <| PubKey("0393e579d9f66d08adb4a28625df70f23bcc5313a87fa4f8142f3748ded8c5f11e")
            ChannelPubKeys = {
                FundingPubKey = FundingPubKey <| PubKey("02cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea")
                RevocationBasepoint = RevocationBasepoint <| PubKey("031b5146ba399994ec342b5073665d294e6dc805539f2aaea7da2ad03e8b9fd105")
                PaymentBasepoint = PaymentBasepoint <| PubKey("026c4d274ec97986e353a6b3ceddcff657bd093c5d9e72d5ea22f7d106ce802e3c")
                DelayedPaymentBasepoint = DelayedPaymentBasepoint <| PubKey("03c3bb2ddc4f9940fc782dec25e5a37d5c34e952ea7311bd878d8f067d96b96c59")
                HtlcBasepoint = HtlcBasepoint <| PubKey("02a9759e73d17a843f398f255279f275076234651f3fd0307b56deee7f0b1a8b4a")
            }
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = LNMoney 1000L
            ToSelfDelay = BlockHeightOffset16 144us
            MaxAcceptedHTLCs = 1000us
            IsFunder = true
            DefaultFinalScriptPubKey = Script("03e2cf26b00d25bfd949072bca0e1d23ead68c0ad81d1e07521d0af181bf1a80ca OP_CHECKSIG")
            Features = FeatureBits.CreateUnsafe([||])
        }
        let remoteParams = {
            NodeId = NodeId <| PubKey("028bbbd3ffd114ee87fb1807028a7edd256d7a534de13b82dea0e1a6eeed5b3a2b")
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = LNMoney 1000L
            ToSelfDelay = BlockHeightOffset16 144us
            MaxAcceptedHTLCs = 1000us
            ChannelPubKeys = {
                FundingPubKey = FundingPubKey <| PubKey("0363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb")
                RevocationBasepoint = RevocationBasepoint <| PubKey("03cf6b3aa11229167394dc082353d7dea4c0ad6826fdaa7f979ca1df4428cdb0ce")
                PaymentBasepoint = PaymentBasepoint <| PubKey("021c49d9da55214545ea2e27029b5cf5419949063b09a36f3a9a9541fdbccb7ec5")
                DelayedPaymentBasepoint = DelayedPaymentBasepoint <| PubKey("02299434e4925e0b937d97968ab1d9b2ac3723b0b93f8ef3b11c7f320a4048f267")
                HtlcBasepoint = HtlcBasepoint <| PubKey("024a044ec805c1f92fd0273414d7a8ca161e94a552e481ead2c24125066469ebf4")
            }
            Features = FeatureBits.CreateUnsafe([||])
            MinimumDepth = BlockHeightOffset32 6u
        }
        let fundingScriptCoin =
            let outpoint = OutPoint.Parse("7ff07760cb7ff712f6ca091b5c58d2777bfc9e6bfdc6b6e5a7824775a967339a-8")
            let txout =
                let amount = 10_000_000L |> Money.Satoshis
                let scriptPubKey = Script("0 fbba6503a67af85fd260d670c09962e6a2ed7c55acd8e39f6e626336be08d337")
                TxOut(amount, scriptPubKey)
            let redeem = Script("2 02cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea 0363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb 2 OP_CHECKMULTISIG")
            ScriptCoin(outpoint, txout, redeem)
        let localChannelPrivKeys = {
            FundingPrivKey = FundingPrivKey <| key("d0d924812151d23d558f5eeb34e6d5f61d7aca9de2d82953d56fb39a4d9baaa7")
            RevocationBasepointSecret = RevocationBasepointSecret <| key("be24a727443fadccfad12b5f01ccaf1db6de2082b1b597919d71e8d917bf79f5")
            PaymentBasepointSecret = PaymentBasepointSecret <| key("939970e056c776ab43a79ceb6e48c8f145358fc256d143200606b67f58618429")
            DelayedPaymentBasepointSecret = DelayedPaymentBasepointSecret <| key("116e5daefba4e8f855469ba2b3b132b9450f1795b2b4a37d0cc559a4498acd8f")
            HtlcBasepointSecret = HtlcBasepointSecret <| key("11812fcdaf404c428eecccca56fd77dceb0bb7712c4d634a960091c37ae9812e")
            CommitmentSeed = CommitmentSeed <| PerCommitmentSecret(key("dfab9aa5d9db263c073d18968e5e96055cd943439b6ddb6d7f24bb1133e809b4"))
        }
        let commitmentTx = Transaction.Parse("020000000001019a3367a9754782a7e5b6c6fd6b9efc7b77d2585c1b09caf612f77fcb6077f07f080000000087aaca8002ce831e0000000000220020e7ffd0ee27c643c475b210269c3a3e68b2bb965dd7437f26964d4f440c98749400127a0000000000160014735640c5850cf2ea840c1b78cbb72ce16e3112f3040047304402201c34b56ca8e9a4004d00d7e4aa5670631965dd1e76c5ae0119c2dcb4f0b6799602200c6e83a4c9d27aec58407a390aff3a5d80f8fc946884020813058e977f47d42101473044022062e8a4d57dd95d1d016974db60f2bb2e53515da79df00fe8dfb8cce8988fefa602202dc78b70cff835cfa82b173c87a85eb3604d8a5549f7dd8acd507bba203c129b0147522102cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea210363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb52aeb0e77d20", n)
        let transactionBuilder =
            ForceCloseFundsRecovery.tryGetFundsFromLocalCommitmentTx
                localParams
                remoteParams
                fundingScriptCoin
                localChannelPrivKeys
                Network.RegTest
                commitmentTx
            |> Result.deref

        let recoveryTransaction =
            let dest =
                let key = new Key()
                key.PubKey
            transactionBuilder
                .SendAll(dest)
                .BuildTransaction(true)
        let inputs = recoveryTransaction.Inputs
        let input = inputs.[0]
        if input.WitScript = WitScript.Empty then
            failwith "witness script is empty"
    *)

    testCase "minimal testcase" <| fun _ ->
        let key(hex: string): Key =
            new Key(Encoders.Hex.DecodeData(hex))
        let localParams = {
            NodeId = NodeId <| PubKey("0393e579d9f66d08adb4a28625df70f23bcc5313a87fa4f8142f3748ded8c5f11e")
            ChannelPubKeys = {
                FundingPubKey = FundingPubKey <| PubKey("02cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea")
                RevocationBasepoint = RevocationBasepoint <| PubKey("031b5146ba399994ec342b5073665d294e6dc805539f2aaea7da2ad03e8b9fd105")
                PaymentBasepoint = PaymentBasepoint <| PubKey("026c4d274ec97986e353a6b3ceddcff657bd093c5d9e72d5ea22f7d106ce802e3c")
                DelayedPaymentBasepoint = DelayedPaymentBasepoint <| PubKey("03c3bb2ddc4f9940fc782dec25e5a37d5c34e952ea7311bd878d8f067d96b96c59")
                HtlcBasepoint = HtlcBasepoint <| PubKey("02a9759e73d17a843f398f255279f275076234651f3fd0307b56deee7f0b1a8b4a")
            }
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = LNMoney 1000L
            ToSelfDelay = BlockHeightOffset16 144us
            MaxAcceptedHTLCs = 1000us
            IsFunder = true
            DefaultFinalScriptPubKey = Script("03e2cf26b00d25bfd949072bca0e1d23ead68c0ad81d1e07521d0af181bf1a80ca OP_CHECKSIG")
            Features = FeatureBits.CreateUnsafe([||])
        }
        let remoteParams = {
            NodeId = NodeId <| PubKey("028bbbd3ffd114ee87fb1807028a7edd256d7a534de13b82dea0e1a6eeed5b3a2b")
            DustLimitSatoshis = 546L |> Money.Satoshis
            MaxHTLCValueInFlightMSat = 10_000_000L |> LNMoney
            ChannelReserveSatoshis = 1000L |> Money.Satoshis
            HTLCMinimumMSat = LNMoney 1000L
            ToSelfDelay = BlockHeightOffset16 144us
            MaxAcceptedHTLCs = 1000us
            ChannelPubKeys = {
                FundingPubKey = FundingPubKey <| PubKey("0363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb")
                RevocationBasepoint = RevocationBasepoint <| PubKey("03cf6b3aa11229167394dc082353d7dea4c0ad6826fdaa7f979ca1df4428cdb0ce")
                PaymentBasepoint = PaymentBasepoint <| PubKey("021c49d9da55214545ea2e27029b5cf5419949063b09a36f3a9a9541fdbccb7ec5")
                DelayedPaymentBasepoint = DelayedPaymentBasepoint <| PubKey("02299434e4925e0b937d97968ab1d9b2ac3723b0b93f8ef3b11c7f320a4048f267")
                HtlcBasepoint = HtlcBasepoint <| PubKey("024a044ec805c1f92fd0273414d7a8ca161e94a552e481ead2c24125066469ebf4")
            }
            Features = FeatureBits.CreateUnsafe([||])
            MinimumDepth = BlockHeightOffset32 6u
        }
        let fundingScriptCoin =
            let outpoint = OutPoint.Parse("7ff07760cb7ff712f6ca091b5c58d2777bfc9e6bfdc6b6e5a7824775a967339a-8")
            let txout =
                let amount = 10_000_000L |> Money.Satoshis
                let scriptPubKey = Script("0 fbba6503a67af85fd260d670c09962e6a2ed7c55acd8e39f6e626336be08d337")
                TxOut(amount, scriptPubKey)
            let redeem = Script("2 02cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea 0363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb 2 OP_CHECKMULTISIG")
            ScriptCoin(outpoint, txout, redeem)
        let localChannelPrivKeys = {
            FundingPrivKey = FundingPrivKey <| key("d0d924812151d23d558f5eeb34e6d5f61d7aca9de2d82953d56fb39a4d9baaa7")
            RevocationBasepointSecret = RevocationBasepointSecret <| key("be24a727443fadccfad12b5f01ccaf1db6de2082b1b597919d71e8d917bf79f5")
            PaymentBasepointSecret = PaymentBasepointSecret <| key("939970e056c776ab43a79ceb6e48c8f145358fc256d143200606b67f58618429")
            DelayedPaymentBasepointSecret = DelayedPaymentBasepointSecret <| key("116e5daefba4e8f855469ba2b3b132b9450f1795b2b4a37d0cc559a4498acd8f")
            HtlcBasepointSecret = HtlcBasepointSecret <| key("11812fcdaf404c428eecccca56fd77dceb0bb7712c4d634a960091c37ae9812e")
            CommitmentSeed = CommitmentSeed <| PerCommitmentSecret(key("dfab9aa5d9db263c073d18968e5e96055cd943439b6ddb6d7f24bb1133e809b4"))
        }
        let commitmentTx = Transaction.Parse("020000000001019a3367a9754782a7e5b6c6fd6b9efc7b77d2585c1b09caf612f77fcb6077f07f080000000087aaca8002ce831e0000000000220020e7ffd0ee27c643c475b210269c3a3e68b2bb965dd7437f26964d4f440c98749400127a0000000000160014735640c5850cf2ea840c1b78cbb72ce16e3112f3040047304402201c34b56ca8e9a4004d00d7e4aa5670631965dd1e76c5ae0119c2dcb4f0b6799602200c6e83a4c9d27aec58407a390aff3a5d80f8fc946884020813058e977f47d42101473044022062e8a4d57dd95d1d016974db60f2bb2e53515da79df00fe8dfb8cce8988fefa602202dc78b70cff835cfa82b173c87a85eb3604d8a5549f7dd8acd507bba203c129b0147522102cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea210363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb52aeb0e77d20", n)
        let transactionBuilder =
            let TxVersionNumberOfCommitmentTxs = 2u
            let obscuredCommitmentNumber =
                if commitmentTx.Version <> TxVersionNumberOfCommitmentTxs then
                    failwith "invalid tx version"
                if commitmentTx.Inputs.Count = 0 then
                    failwith "tx has no inputs"
                if commitmentTx.Inputs.Count > 1 then
                    failwith "tx has multiple inputs"
                let txIn = Seq.exactlyOne commitmentTx.Inputs
                if fundingScriptCoin.Outpoint <> txIn.PrevOut then
                    failwith "does not spend channel funds"
                match ObscuredCommitmentNumber.TryFromLockTimeAndSequence commitmentTx.LockTime txIn.Sequence with
                | None ->
                    failwith "invalid lock time and sequence"
                | Some obscuredCommitmentNumber ->
                    obscuredCommitmentNumber
            let localChannelPubKeys = localParams.ChannelPubKeys
            let remoteChannelPubKeys = remoteParams.ChannelPubKeys
            let commitmentNumber =
                obscuredCommitmentNumber.Unobscure
                    localParams.IsFunder
                    localChannelPubKeys.PaymentBasepoint
                    remoteChannelPubKeys.PaymentBasepoint

            let perCommitmentPoint =
                localChannelPrivKeys.CommitmentSeed.DerivePerCommitmentPoint commitmentNumber
            let localCommitmentPubKeys =
                perCommitmentPoint.DeriveCommitmentPubKeys localChannelPubKeys
            let remoteCommitmentPubKeys =
                perCommitmentPoint.DeriveCommitmentPubKeys remoteChannelPubKeys

            let transactionBuilder = n.CreateTransactionBuilder()

            let toLocalScriptPubKey =
                Scripts.toLocalDelayed
                    remoteCommitmentPubKeys.RevocationPubKey
                    localParams.ToSelfDelay
                    localCommitmentPubKeys.DelayedPaymentPubKey
            let toLocalIndexOpt =
                let toLocalWitScriptPubKey = toLocalScriptPubKey.WitHash.ScriptPubKey
                Seq.tryFindIndex
                    (fun (txOut: TxOut) -> txOut.ScriptPubKey = toLocalWitScriptPubKey)
                    commitmentTx.Outputs
            let toLocalIndex =
                match toLocalIndexOpt with
                | Some toLocalIndex -> toLocalIndex
                | None -> failwith "balance below dust limit"

            let delayedPaymentPrivKey =
                perCommitmentPoint.DeriveDelayedPaymentPrivKey
                    localChannelPrivKeys.DelayedPaymentBasepointSecret

            transactionBuilder
                .SetVersion(TxVersionNumberOfCommitmentTxs)
                .Extensions.Add(CommitmentToLocalExtension())
            transactionBuilder
                .AddKeys(delayedPaymentPrivKey.RawKey())
                .AddCoin(
                    ScriptCoin(commitmentTx, uint32 toLocalIndex, toLocalScriptPubKey),
                    CoinOptions(
                        Sequence = (Nullable <| Sequence(uint32 localParams.ToSelfDelay.Value))
                    )
                )

        let recoveryTransaction =
            let dest =
                let key = new Key()
                key.PubKey
            transactionBuilder
                .SendAll(dest)
                .BuildTransaction(true)
        let inputs = recoveryTransaction.Inputs
        let input = inputs.[0]
        if input.WitScript = WitScript.Empty then
            failwith "witness script is empty"
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
