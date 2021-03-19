namespace DotNetLightning.Utils

open ResultUtils
open ResultUtils.Portability

// A SeqConsumer is a parser for sequences. It wraps a function which takes a
// sequence and optionally returns a value successfully-parsed from the start of
// the sequence along with the rest of the sequence. If parsing fails it returns
// None.
// 
// You can construct a SeqConsumer using the seqConsumer compuation expression.
// The bind operation of the computation expression will return the value parsed
// from the sequence and advance the sequence to the position where the next value
// can be parsed from. For example, given a parser parseValue, you can construct a
// parser which parses three values like this:
// 
// seqConsumer {
//    let! value0 = parseValue()
//    let! value1 = parseValue()
//    let! value2 = parseValue()
//    return (value0, value1, value2)
// }
// 
// You can also call SeqConsumer.next and SeqConsumer.abort from within a
// seqConsumer to pop the next element of the sequence or to abort parsing
// respectively.
// 
// The function SeqConsumer.ConsumeAll takes a sequence and a SeqConsumer and will
// attempt to parse the sequence to completion.

[<AutoOpen>]
module SeqConsumerCE =
    type SeqConsumer<'SeqElement, 'Value> = {
        Consume: seq<'SeqElement> -> Option<seq<'SeqElement> * 'Value>
    }

    type SeqConsumerBuilder<'SeqElement>() =
        member __.Bind<'Arg, 'Return>(seqConsumer0: SeqConsumer<'SeqElement, 'Arg>,
                                      func: 'Arg -> SeqConsumer<'SeqElement, 'Return>
                                     ): SeqConsumer<'SeqElement, 'Return> = {
            Consume = fun (sequence0: seq<'SeqElement>) ->
                match seqConsumer0.Consume sequence0 with
                | None -> None
                | Some (sequence1, value0) ->
                    let seqConsumer1 = func value0
                    seqConsumer1.Consume sequence1
        }

        member __.Return<'Value>(value: 'Value)
                                    : SeqConsumer<'SeqElement, 'Value> = {
            Consume = fun (sequence: seq<'SeqElement>) -> Some (sequence, value)
        }

        member __.ReturnFrom<'Value>(seqConsumer: SeqConsumer<'SeqElement, 'Value>)
                                        : SeqConsumer<'SeqElement, 'Value> =
            seqConsumer

        member __.Zero(): SeqConsumer<'SeqElement, unit> = {
            Consume = fun (sequence: seq<'SeqElement>) -> Some (sequence, ())
        }

        member __.Delay<'Value>(delayedSeqConsumer: unit -> SeqConsumer<'SeqElement, 'Value>)
                                   : SeqConsumer<'SeqElement, 'Value> = {
            Consume = fun (sequence: seq<'SeqElement>) ->
                (delayedSeqConsumer ()).Consume sequence
        }

        member __.TryWith<'Value>(seqConsumer: SeqConsumer<'SeqElement, 'Value>,
                                  onException: exn -> SeqConsumer<'SeqElement, 'Value>
                                 ): SeqConsumer<'SeqElement, 'Value> = {
            Consume = fun (sequence: seq<'SeqElement>) ->
                try
                    seqConsumer.Consume sequence
                with
                | ex ->
                    let subSeqConsumer = onException ex
                    subSeqConsumer.Consume sequence
        }

    let seqConsumer<'SeqElement> = SeqConsumerBuilder<'SeqElement>()

[<RequireQualifiedAccess>]
module SeqConsumer =
    let next<'SeqElement>(): SeqConsumer<'SeqElement, 'SeqElement> = {
        Consume = fun (sequence: seq<'SeqElement>) ->
            Seq.tryHead sequence
            |> Option.map (fun value -> (Seq.tail sequence, value))
    }

    let abort<'SeqElement, 'Value>(): SeqConsumer<'SeqElement, 'Value> = {
        Consume = fun (_sequence: seq<'SeqElement>) -> None
    }

    type ConsumeAllError =
        | SequenceEndedTooEarly
        | SequenceNotReadToEnd

    let consumeAll<'SeqElement, 'Value> (sequence: seq<'SeqElement>)
                                        (seqConsumer: SeqConsumer<'SeqElement, 'Value>)
                                            : Result<'Value, ConsumeAllError> =
        match seqConsumer.Consume sequence with
        | None -> Error SequenceEndedTooEarly
        | Some (consumedSequence, value) ->
            if Seq.isEmpty consumedSequence then
                Ok value
            else
                Error SequenceNotReadToEnd


