namespace DotNetLightning.Channel

open NBitcoin
open DotNetLightning.Utils
open DotNetLightning.Utils.Aether
open DotNetLightning.Utils.Aether.Operators
open DotNetLightning.Crypto
open DotNetLightning.Transactions
open DotNetLightning.Serialization.Msgs

open ResultUtils
open ResultUtils.Portability

type LocalChanges = {
    Proposed: IUpdateMsg list
    Signed: IUpdateMsg list
    ACKed: IUpdateMsg list
}
    with
        static member Zero = { Proposed = []; Signed = []; ACKed = [] }

        // -- lenses
        static member Proposed_: Lens<_, _> =
            (fun lc -> lc.Proposed),
            (fun ps lc -> { lc with Proposed = ps })
        static member Signed_: Lens<_, _> =
            (fun lc -> lc.Signed),
            (fun v lc -> { lc with Signed = v })

        static member ACKed_: Lens<_, _> =
            (fun lc -> lc.ACKed),
            (fun v lc -> { lc with ACKed = v })

        member this.All() =
            this.Proposed @ this.Signed @ this.ACKed

type RemoteChanges = { 
    Proposed: IUpdateMsg list
    Signed: IUpdateMsg list
    ACKed: IUpdateMsg list
}
    with
        static member Zero = { Proposed = []; Signed = []; ACKed = [] }
        static member Proposed_: Lens<_, _> =
            (fun lc -> lc.Proposed),
            (fun ps lc -> { lc with Proposed = ps })
        static member Signed_: Lens<_, _> =
            (fun lc -> lc.Signed),
            (fun v lc -> { lc with Signed = v })

        static member ACKed_: Lens<_, _> =
            (fun lc -> lc.ACKed),
            (fun v lc -> { lc with ACKed = v })


type PublishableTxs = {
    CommitTx: FinalizedTx
    HTLCTxs: FinalizedTx list
}

type LocalCommit = {
    Index: CommitmentNumber
    Spec: CommitmentSpec
    PublishableTxs: PublishableTxs
    /// These are not redeemable on-chain until we get a corresponding preimage.
    PendingHTLCSuccessTxs: HTLCSuccessTx list
}
type RemoteCommit = {
    Index: CommitmentNumber
    Spec: CommitmentSpec
    RemotePerCommitmentPoint: PerCommitmentPoint
}

type RemoteNextCommitInfo =
    | Waiting of RemoteCommit
    | Revoked of PerCommitmentPoint
    with
        static member Waiting_: Prism<RemoteNextCommitInfo, RemoteCommit> =
            (fun remoteNextCommitInfo ->
                match remoteNextCommitInfo with
                | Waiting remoteCommit -> Some remoteCommit
                | Revoked _ -> None),
            (fun waitingForRevocation remoteNextCommitInfo ->
                match remoteNextCommitInfo with
                | Waiting _ -> Waiting waitingForRevocation
                | Revoked _ -> remoteNextCommitInfo)

        static member Revoked_: Prism<RemoteNextCommitInfo, PerCommitmentPoint> =
            (fun remoteNextCommitInfo ->
                match remoteNextCommitInfo with
                | Waiting _ -> None
                | Revoked commitmentPubKey -> Some commitmentPubKey),
            (fun commitmentPubKey remoteNextCommitInfo ->
                match remoteNextCommitInfo with
                | Waiting _ -> remoteNextCommitInfo
                | Revoked _ -> Revoked commitmentPubKey)

        member self.PerCommitmentPoint(): PerCommitmentPoint =
            match self with
            | Waiting remoteCommit -> remoteCommit.RemotePerCommitmentPoint
            | Revoked perCommitmentPoint -> perCommitmentPoint

type Commitments = {
    LocalCommit: LocalCommit
    RemoteCommit: RemoteCommit
    LocalChanges: LocalChanges
    RemoteChanges: RemoteChanges
    LocalNextHTLCId: HTLCId
    RemoteNextHTLCId: HTLCId
    OriginChannels: Map<HTLCId, HTLCSource>
}
    with
        static member LocalChanges_: Lens<_, _> =
            (fun c -> c.LocalChanges),
            (fun v c -> { c with LocalChanges = v })
        static member RemoteChanges_: Lens<_, _> =
            (fun c -> c.RemoteChanges),
            (fun v c -> { c with RemoteChanges = v })

        member this.AddLocalProposal(proposal: IUpdateMsg) =
            let lens = Commitments.LocalChanges_ >-> LocalChanges.Proposed_
            Optic.map lens (fun proposalList -> proposal :: proposalList) this

        member this.AddRemoteProposal(proposal: IUpdateMsg) =
            let lens = Commitments.RemoteChanges_ >-> RemoteChanges.Proposed_
            Optic.map lens (fun proposalList -> proposal :: proposalList) this

        member this.IncrLocalHTLCId() = { this with LocalNextHTLCId = this.LocalNextHTLCId + 1UL }
        member this.IncrRemoteHTLCId() = { this with RemoteNextHTLCId = this.RemoteNextHTLCId + 1UL }

        member this.LocalHasChanges() =
            (not this.RemoteChanges.ACKed.IsEmpty) || (not this.LocalChanges.Proposed.IsEmpty)

        member this.RemoteHasChanges() =
            (not this.LocalChanges.ACKed.IsEmpty) || (not this.RemoteChanges.Proposed.IsEmpty)

        member internal this.LocalHasUnsignedOutgoingHTLCs() =
            this.LocalChanges.Proposed |> List.exists(fun p -> match p with | :? UpdateAddHTLCMsg -> true | _ -> false)

        member internal this.RemoteHasUnsignedOutgoingHTLCs() =
            this.RemoteChanges.Proposed |> List.exists(fun p -> match p with | :? UpdateAddHTLCMsg -> true | _ -> false)

        member internal this.HasNoPendingHTLCs (remoteNextCommitInfo: RemoteNextCommitInfo) =
            this.LocalCommit.Spec.OutgoingHTLCs.IsEmpty
            && this.LocalCommit.Spec.IncomingHTLCs.IsEmpty
            && this.RemoteCommit.Spec.OutgoingHTLCs.IsEmpty
            && this.RemoteCommit.Spec.IncomingHTLCs.IsEmpty
            && (remoteNextCommitInfo |> function Waiting _ -> false | Revoked _ -> true)

        member internal this.GetOutgoingHTLCCrossSigned (remoteNextCommitInfo: RemoteNextCommitInfo)
                                                        (htlcId: HTLCId)
                                                            : Option<UpdateAddHTLCMsg> =
            let remoteSigned =
                Map.tryFind htlcId this.LocalCommit.Spec.OutgoingHTLCs
            let localSigned =
                let remoteCommit =
                    match remoteNextCommitInfo with
                    | Revoked _ -> this.RemoteCommit
                    | Waiting nextRemoteCommit -> nextRemoteCommit
                Map.tryFind htlcId remoteCommit.Spec.IncomingHTLCs
            match remoteSigned, localSigned with
            | Some _, Some htlcIn -> htlcIn |> Some
            | _ -> None

        member internal this.GetIncomingHTLCCrossSigned (remoteNextCommitInfo: RemoteNextCommitInfo)
                                                        (htlcId: HTLCId)
                                                            : Option<UpdateAddHTLCMsg> =
            let remoteSigned =
                Map.tryFind htlcId this.LocalCommit.Spec.IncomingHTLCs
            let localSigned =
                let remoteCommit =
                    match remoteNextCommitInfo with
                    | Revoked _ -> this.RemoteCommit
                    | Waiting nextRemoteCommit -> nextRemoteCommit
                Map.tryFind htlcId remoteCommit.Spec.OutgoingHTLCs
            match remoteSigned, localSigned with
            | Some _, Some htlcIn -> htlcIn |> Some
            | _ -> None

        member this.SpendableBalance (localIsFunder: bool)
                                     (remoteParams: RemoteParams)
                                     (remoteNextCommitInfo: RemoteNextCommitInfo)
                                         : LNMoney =
            let remoteCommit =
                match remoteNextCommitInfo with
                | RemoteNextCommitInfo.Waiting nextRemoteCommit -> nextRemoteCommit
                | RemoteNextCommitInfo.Revoked _info -> this.RemoteCommit
            let reducedRes =
                remoteCommit.Spec.Reduce(
                    this.RemoteChanges.ACKed,
                    this.LocalChanges.Proposed
                )
            let reduced =
                match reducedRes with
                | Error err ->
                    failwithf
                        "reducing commit failed even though we have not proposed any changes\
                        error: %A"
                        err
                | Ok reduced -> reduced
            let fees =
                if localIsFunder then
                    Transactions.commitTxFee remoteParams.DustLimitSatoshis reduced
                    |> LNMoney.FromMoney
                else
                    LNMoney.Zero
            let channelReserve =
                remoteParams.ChannelReserveSatoshis
                |> LNMoney.FromMoney
            let totalBalance = reduced.ToRemote
            let untrimmedSpendableBalance = totalBalance - channelReserve - fees
            let dustLimit =
                remoteParams.DustLimitSatoshis
                |> LNMoney.FromMoney
            let untrimmedMax = LNMoney.Min(untrimmedSpendableBalance, dustLimit)
            let spendableBalance = LNMoney.Max(untrimmedMax, untrimmedSpendableBalance)
            spendableBalance
