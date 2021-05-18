module TransactionTests

open System
open ResultUtils
open ResultUtils.Portability

open DotNetLightning.Utils
open Expecto
open NBitcoin
open NBitcoin.BuilderExtensions
open NBitcoin.DataEncoders
open NBitcoin.Crypto

let localToSelfDelay = 144us
let remoteRevocationPubKey = PubKey("03fba995b08bc50430cdc6088162433154904219ac30f34a544f8b5d91fcf3219b")
let localDelayedPaymentPubKey = PubKey("0388fb0e96738992a0c0c8c853f6c1548a77c81163f6733e4bcc1c17f110f1905b")
let toLocalScriptPubKey =
    let opList = ResizeArray<Op>()
    opList.Add(!> OpcodeType.OP_IF)
    opList.Add(Op.GetPushOp(remoteRevocationPubKey.ToBytes()))
    opList.Add(!> OpcodeType.OP_ELSE)
    opList.Add(Op.GetPushOp(int64 localToSelfDelay))
    opList.Add(!> OpcodeType.OP_CHECKSEQUENCEVERIFY)
    opList.Add(!> OpcodeType.OP_DROP)
    opList.Add(Op.GetPushOp(localDelayedPaymentPubKey.ToBytes()))
    opList.Add(!> OpcodeType.OP_ENDIF)
    opList.Add(!> OpcodeType.OP_CHECKSIG)
    Script(opList)

type WowCommitmentToLocalParameters = {
    RevocationPubKey: PubKey
    ToSelfDelay: uint16
    LocalDelayedPubKey: PubKey
}
    with
    static member TryExtractParameters (scriptPubKey: Script): Option<WowCommitmentToLocalParameters> =
        if scriptPubKey = toLocalScriptPubKey then
            let data = {
                RevocationPubKey = PubKey("03fba995b08bc50430cdc6088162433154904219ac30f34a544f8b5d91fcf3219b")
                ToSelfDelay = 144us
                LocalDelayedPubKey = PubKey("0388fb0e96738992a0c0c8c853f6c1548a77c81163f6733e4bcc1c17f110f1905b")
            }
            Some data
        else
            None

type internal WowCommitmentToLocalExtension() =
    inherit BuilderExtension()
        override self.CanGenerateScriptSig (scriptPubKey: Script): bool =
            scriptPubKey = toLocalScriptPubKey

        override self.GenerateScriptSig(scriptPubKey: Script, keyRepo: IKeyRepository, signer: ISigner): Script =
            assert (scriptPubKey = toLocalScriptPubKey)
            let parameters = {
                RevocationPubKey = PubKey("03fba995b08bc50430cdc6088162433154904219ac30f34a544f8b5d91fcf3219b")
                ToSelfDelay = 144us
                LocalDelayedPubKey = PubKey("0388fb0e96738992a0c0c8c853f6c1548a77c81163f6733e4bcc1c17f110f1905b")
            }
            let pubKey = keyRepo.FindKey scriptPubKey
            Console.WriteLine(sprintf "pubKey == %A" pubKey)
            // FindKey will return null if it can't find a key for
            // scriptPubKey. If we can't find a valid key then this method
            // should return null, indicating to NBitcoin that the sigScript
            // could not be generated.
            match pubKey with
            | null -> null
            | _ when pubKey = parameters.RevocationPubKey ->
                let revocationSig = signer.Sign (parameters.RevocationPubKey)
                Script [
                    Op.GetPushOp (revocationSig.ToBytes())
                    Op.op_Implicit OpcodeType.OP_TRUE
                ]
            | _ when pubKey = parameters.LocalDelayedPubKey ->
                let localDelayedSig = signer.Sign (parameters.LocalDelayedPubKey)
                Script [
                    Op.GetPushOp (localDelayedSig.ToBytes())
                    Op.op_Implicit OpcodeType.OP_FALSE
                ]
            | _ -> null

        override self.CanDeduceScriptPubKey(_scriptSig: Script): bool =
            false

        override self.DeduceScriptPubKey(_scriptSig: Script): Script =
            raise <| NotSupportedException()

        override self.CanEstimateScriptSigSize(_scriptPubKey: Script): bool =
            false

        override self.EstimateScriptSigSize(_scriptPubKey: Script): int =
            raise <| NotSupportedException()

        override self.CanCombineScriptSig(_scriptPubKey: Script, _a: Script, _b: Script): bool = 
            false

        override self.CombineScriptSig(_scriptPubKey: Script, _a: Script, _b: Script): Script =
            raise <| NotSupportedException()

        override self.IsCompatibleKey(pubKey: PubKey, scriptPubKey: Script): bool =
            match WowCommitmentToLocalParameters.TryExtractParameters scriptPubKey with
            | None -> false
            | Some parameters ->
                parameters.RevocationPubKey = pubKey
                || parameters.LocalDelayedPubKey = pubKey


let n = Network.RegTest

[<Tests>]
let testList = testList "transaction tests" [
    testCase "minimal testcase" <| fun _ ->
        let key(hex: string): Key =
            new Key(Encoders.Hex.DecodeData(hex))
        let commitmentTx = Transaction.Parse("020000000001019a3367a9754782a7e5b6c6fd6b9efc7b77d2585c1b09caf612f77fcb6077f07f080000000087aaca8002ce831e0000000000220020e7ffd0ee27c643c475b210269c3a3e68b2bb965dd7437f26964d4f440c98749400127a0000000000160014735640c5850cf2ea840c1b78cbb72ce16e3112f3040047304402201c34b56ca8e9a4004d00d7e4aa5670631965dd1e76c5ae0119c2dcb4f0b6799602200c6e83a4c9d27aec58407a390aff3a5d80f8fc946884020813058e977f47d42101473044022062e8a4d57dd95d1d016974db60f2bb2e53515da79df00fe8dfb8cce8988fefa602202dc78b70cff835cfa82b173c87a85eb3604d8a5549f7dd8acd507bba203c129b0147522102cea9c626fb7bc6e287bc4c46aecf0d37ed894054035f6e2d00c823b9bfad36ea210363e6b4599dac321d7d270c12ba9dbf6ea8fef0c1c2f3707371a7e16a37c084eb52aeb0e77d20", n)
        let transactionBuilder =
            let transactionBuilder = n.CreateTransactionBuilder()

            let toLocalIndex = 0u
            let delayedPaymentPrivKey = key("a7fd56eeb68a783843049791c430b77d234d18a85e607ab4210e659c472ac476")
            transactionBuilder
                .SetVersion(2u)
                .Extensions.Add(WowCommitmentToLocalExtension())
            transactionBuilder
                .AddKeys(delayedPaymentPrivKey)
                .AddCoin(
                    ScriptCoin(commitmentTx, toLocalIndex, toLocalScriptPubKey),
                    CoinOptions(
                        Sequence = (Nullable <| Sequence(uint32 localToSelfDelay))
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
]
