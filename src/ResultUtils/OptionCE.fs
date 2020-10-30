namespace ResultUtils

open System

[<AutoOpen>]
module OptionCE =
  type OptionBuilder() =
    member __.Return (v: 'T) : Option<'T> =
      Some v

    member __.ReturnFrom (opt: Option<'T>) : Option<'T> =
      opt

    member this.Zero () : Option<unit> =
      this.Return ()

    member __.Bind
        (opt: Option<'T>, binder: 'T -> Option<'U>)
        : Option<'U> =
      Option.bind binder opt

    member __.Delay
        (generator: unit -> Option<'T>)
        : unit -> Option<'T> =
      generator

    member __.Run
        (generator: unit -> Option<'T>)
        : Option<'T> =
      generator ()

    member this.Combine
        (opt: Option<unit>, binder: unit -> Option<'T>)
        : Option<'T> =
      this.Bind(opt, binder)

    member this.TryWith
        (generator: unit -> Option<'T>,
         handler: exn -> Option<'T>)
        : Option<'T> =
      try this.Run generator with | e -> handler e

    member this.TryFinally
        (generator: unit -> Option<'T>, compensation: unit -> unit)
        : Option<'T> =
      try this.Run generator finally compensation ()

    member this.Using
        (resource: 'T when 'T :> IDisposable, binder: 'T -> Option<'U>)
        : Option<'U> =
      this.TryFinally (
        (fun () -> binder resource),
        (fun () -> if not <| obj.ReferenceEquals(resource, null) then resource.Dispose ())
      )

    member this.While
        (guard: unit -> bool, generator: unit -> Option<unit>)
        : Option<unit> =
      if not <| guard () then this.Zero ()
      else this.Bind(this.Run generator, fun () -> this.While (guard, generator))

    member this.For
        (sequence: #seq<'T>, binder: 'T -> Option<unit>)
        : Option<unit> =
      this.Using(sequence.GetEnumerator (), fun enum ->
        this.While(enum.MoveNext,
          this.Delay(fun () -> binder enum.Current)))

[<AutoOpen>]
module OptionCEExtensions =
  let option = OptionBuilder()

