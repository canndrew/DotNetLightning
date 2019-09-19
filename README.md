
## How to configure

Configuration will follow an usual semantics for ASP.NET Core web app.
That is, it will read in following order.

* Read `appsettings.json`

## Supported platfrom

It should work on every os if we build Secp256k1 for every platfrom.
Right now. It only supports Linux.

## Things what we are not going to do

* Support .NET Framework ... Since we heavily rely on `Span<T>` for binary serialization.

## TODO

* Refactor PeerChannelEncryptor to optimize performance (Probably by using byref-like-type and mutable state)
* Update `Channel.deriveOurDustLimitSatoshis`

