namespace DotNetLightning.Channel

open NBitcoin
open NBitcoin.BuilderExtensions
open DotNetLightning.Utils
open DotNetLightning.Utils.SeqConsumer
open DotNetLightning.Crypto

type CommitmentToLocalParameters = {
    RevocationPubKey: RevocationPubKey
    ToSelfDelay: BlockHeightOffset16
    LocalDelayedPubKey: DelayedPaymentPubKey
}
    with
    static member TryExtractParameters (scriptPubKey: Script): Option<CommitmentToLocalParameters> =
        let ops = scriptPubKey.ToOps()
        let checkOpCode(opcodeType: OpcodeType) = seqConsumer<Op> {
            let! op = Next()
            if op.Code = opcodeType then
                return ()
            else
                return! Abort()
        }
        let consumeAllResult =
            SeqConsumer.ConsumeAll ops <| seqConsumer {
                do! checkOpCode OpcodeType.OP_IF
                let! opRevocationPubKey = Next()
                let! revocationPubKey = seqConsumer {
                    match opRevocationPubKey.PushData with
                    | null -> return! Abort()
                    | bytes -> return RevocationPubKey.FromBytes bytes      // FIXME: catch exception
                }
                do! checkOpCode OpcodeType.OP_ELSE
                let! opToSelfDelay = Next()
                let! toSelfDelay = seqConsumer {
                    let nullableToSelfDelay = opToSelfDelay.GetLong()
                    if nullableToSelfDelay.HasValue then
                        // FIXME: catch exception
                        return BlockHeightOffset16 (uint16 nullableToSelfDelay.Value)
                    else
                        return! Abort()
                }
                do! checkOpCode OpcodeType.OP_CHECKSEQUENCEVERIFY
                do! checkOpCode OpcodeType.OP_DROP
                let! opLocalDelayedPubKey = Next()
                let! localDelayedPubKey = seqConsumer {
                    match opLocalDelayedPubKey.PushData with
                    | null -> return! Abort()
                    | bytes -> return DelayedPaymentPubKey.FromBytes bytes  // FIXME: catch exception
                }
                do! checkOpCode OpcodeType.OP_ENDIF
                do! checkOpCode OpcodeType.OP_CHECKSIG
                return {
                    RevocationPubKey = revocationPubKey
                    ToSelfDelay = toSelfDelay
                    LocalDelayedPubKey = localDelayedPubKey
                }
            }
        match consumeAllResult with
        | Ok data -> Some data
        | Error _ -> None

type internal CommitmentToLocalExtension() =
    inherit BuilderExtension()
        override self.CanGenerateScriptSig (scriptPubKey: Script): bool =
            (CommitmentToLocalParameters.TryExtractParameters scriptPubKey).IsSome

        override self.GenerateScriptSig(scriptPubKey: Script, keyRepo: IKeyRepository, signer: ISigner): Script =
            let parameters =
                match (CommitmentToLocalParameters.TryExtractParameters scriptPubKey) with
                | Some parameters -> parameters
                | None ->
                    failwith
                        "NBitcoin should not call this unless CanGenerateScriptSig returns true"
            let nullableRevocationPubKey = 
                keyRepo.FindKey(parameters.RevocationPubKey.RawPubKey().ScriptPubKey)
            match nullableRevocationPubKey with
            | null ->
                let nullableLocalDelayedPubKey =
                    keyRepo.FindKey(parameters.LocalDelayedPubKey.RawPubKey().ScriptPubKey)
                match nullableLocalDelayedPubKey with
                | null -> null
                | localDelayedPubKey ->
                    let localDelayedSig = signer.Sign localDelayedPubKey
                    Script [
                        Op.GetPushOp (localDelayedSig.ToBytes())
                        Op.op_Implicit OpcodeType.OP_FALSE
                    ]
            | revocationPubKey ->
                let revocationSig = signer.Sign revocationPubKey
                Script [
                    Op.GetPushOp (revocationSig.ToBytes())
                    Op.op_Implicit OpcodeType.OP_TRUE
                ]

        override self.CanDeduceScriptPubKey(_scriptSig: Script): bool =
            false

        override self.DeduceScriptPubKey(_scriptSig: Script): Script =
            raise <| System.NotSupportedException()

        override self.CanEstimateScriptSigSize(_scriptPubKey: Script): bool =
            false

        override self.EstimateScriptSigSize(_scriptPubKey: Script): int =
            raise <| System.NotSupportedException()

        override self.CanCombineScriptSig(_scriptPubKey: Script, _a: Script, _b: Script): bool = 
            false

        override self.CombineScriptSig(_scriptPubKey: Script, _a: Script, _b: Script): Script =
            raise <| System.NotSupportedException()

        override self.IsCompatibleKey(pubKey: PubKey, scriptPubKey: Script): bool =
            match CommitmentToLocalParameters.TryExtractParameters scriptPubKey with
            | None -> false
            | Some parameters ->
                parameters.RevocationPubKey.RawPubKey() = pubKey
                || parameters.LocalDelayedPubKey.RawPubKey() = pubKey


