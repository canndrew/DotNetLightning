namespace DotNetLightning.Utils
open System
open System.Globalization
open NBitcoin

[<Flags>]
type LNMoneyUnit =
    | BTC = 100000000000UL
    | MilliBTC = 100000000UL
    | Bit = 100000UL
    | Micro = 100000UL
    | Satoshi = 1000UL
    | Nano = 100UL
    | MilliSatoshi = 1UL
    | Pico = 1UL

/// Port from `LightMoney` class in BTCPayServer.Lightning
/// Represents millisatoshi amount of money
///
/// Why not use the package directly? because it might cause circular dependency in the future.
/// (i.e. We might want to support this package in BTCPayServer.Lightning)
/// refs: https://github.com/btcpayserver/BTCPayServer.Lightning/blob/f65a883a63bf607176a3b7b0baa94527ac592f5e/src/BTCPayServer.Lightning.Common/LightMoney.cs
[<Struct>]
type LNMoney = {
    MilliSatoshi: int64
} with
    static member private BitcoinStyle =
        NumberStyles.AllowLeadingWhite ||| NumberStyles.AllowTrailingWhite |||
        NumberStyles.AllowLeadingSign ||| NumberStyles.AllowDecimalPoint
    // --- constructors -----
    static member private CheckMoneyUnit(v: LNMoneyUnit, paramName: string) =
        let typeOfMoneyUnit = typeof<LNMoneyUnit>
        if not (Enum.IsDefined(typeOfMoneyUnit, v)) then
            raise (ArgumentException(sprintf "Invalid value for MoneyUnit %s" paramName))

    static member private FromUnit(amount: decimal, lnUnit: LNMoneyUnit) =
        LNMoney.CheckMoneyUnit(lnUnit, "unit") |> ignore
        let msat = Checked.int64 <| Checked.op_Multiply (amount) (decimal lnUnit)
        LNMoney.MilliSatoshis msat

    static member FromMoney (money: Money) =
        LNMoney.Satoshis money.Satoshi

    static member Coins(coins: decimal) =
        LNMoney.FromUnit(coins * (decimal LNMoneyUnit.BTC), LNMoneyUnit.MilliSatoshi)

    static member Satoshis(satoshis: decimal) =
        LNMoney.FromUnit(satoshis * (decimal LNMoneyUnit.Satoshi), LNMoneyUnit.MilliSatoshi)

    static member Satoshis(sats: int64) =
        LNMoney.MilliSatoshis(Checked.op_Multiply 1000L sats)

    static member Satoshis(sats: uint64) =
        LNMoney.Satoshis(Checked.int64 sats)

    static member MilliSatoshis(msats: int64) =
        { MilliSatoshi = msats }

    static member MilliSatoshis(msats: uint64) =
        { MilliSatoshi = Checked.int64 msats }

    static member Zero = LNMoney.MilliSatoshis 0L
    static member One = LNMoney.MilliSatoshis 1L
    static member TryParse(bitcoin: string, result: outref<LNMoney>) =
        match Decimal.TryParse(bitcoin, LNMoney.BitcoinStyle, CultureInfo.InvariantCulture) with
        | false, _ -> false
        | true, v ->
            try
                result <- LNMoney.FromUnit(v, LNMoneyUnit.BTC)
                true
            with
                | :? OverflowException -> false
    static member Parse(bitcoin: string) =
        match LNMoney.TryParse(bitcoin) with
        | true, v -> v
        | _ -> raise (FormatException("Impossible to parse the string in a bitcoin amount"))

    // -------- Arithmetic operations
    static member (+) (a: LNMoney, b: LNMoney) = LNMoney.MilliSatoshis (a.MilliSatoshi + b.MilliSatoshi)
    static member (-) (a: LNMoney, b: LNMoney) = LNMoney.MilliSatoshis (a.MilliSatoshi - b.MilliSatoshi)
    static member (*) (a: LNMoney, b: int64) = LNMoney.MilliSatoshis (a.MilliSatoshi * b)
    static member (*) (a: int64, b: LNMoney) = LNMoney.MilliSatoshis (a * b.MilliSatoshi)
    static member (/) (a: LNMoney, b: int64) = LNMoney.MilliSatoshis (a.MilliSatoshi / b)
    static member (/) (a: LNMoney, b: LNMoney) = a.MilliSatoshi / b.MilliSatoshi
    static member Max(a: LNMoney, b: LNMoney) = if a.MilliSatoshi >= b.MilliSatoshi then a else b
    static member Min(a: LNMoney, b: LNMoney) = if a.MilliSatoshi <= b.MilliSatoshi then a else b
    
    static member MaxValue =
        let maxSatoshis = 2099999997690000L
        LNMoney.Satoshis maxSatoshis

    // --------- Utilities
    member this.Abs() =
        if this < LNMoney.Zero then LNMoney.MilliSatoshis(-this.MilliSatoshi) else this

    member this.SatoshiRoundDown(): int64 = this.MilliSatoshi / (int64 LNMoneyUnit.Satoshi)
    member this.BTCRoundDown(): int64 = this.MilliSatoshi / (int64 LNMoneyUnit.BTC)

    member this.Satoshi(): decimal = (decimal this.MilliSatoshi) / (decimal LNMoneyUnit.Satoshi)
    member this.BTC(): decimal = (decimal this.MilliSatoshi) / (decimal LNMoneyUnit.BTC)

    member this.ToMoney() = this.SatoshiRoundDown() |> Money

    member this.Split(parts: int): LNMoney seq =
        if parts <= 0 then
            raise (ArgumentOutOfRangeException("parts"))
        else
            let mutable remain = 0L
            let res = Math.DivRem(this.MilliSatoshi, int64 parts, &remain)
            seq {
                for _ in 0..(parts - 1) do
                    yield LNMoney.Satoshis (decimal (res + (if remain > 0L then 1L else 0L)))
                    remain <- remain - 1L
            }
