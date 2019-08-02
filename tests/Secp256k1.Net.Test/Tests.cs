using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Secp256k1Net.Test
{
    public class Tests
    {
        ref struct KeyPair
        {
            public Span<byte> PrivateKey;
            public Span<byte> PublicKey;
        }

        Span<byte> GeneratePrivateKey(Secp256k1 secp256k1)
        {
            var rnd = RandomNumberGenerator.Create();
            Span<byte> privateKey = new byte[32];
            do
            {
                rnd.GetBytes(privateKey);
            }
            while (!secp256k1.SecretKeyVerify(privateKey));
            return privateKey;
        }

        KeyPair GenerateKeyPair(Secp256k1 secp256k1)
        {
            var privateKey = GeneratePrivateKey(secp256k1);
            if (!secp256k1.PublicKeyCreate(privateKey, out var publicKey))
            {
                throw new Exception("Public key creation failed");
            }
            return new KeyPair { PrivateKey = privateKey, PublicKey = publicKey };
        }

        [Fact]
        public void EcdhTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var kp1 = GenerateKeyPair(secp256k1);
                var kp2 = GenerateKeyPair(secp256k1);

                Assert.True(secp256k1.Ecdh(kp1.PublicKey, kp2.PrivateKey, out var sec1));

                Assert.True(secp256k1.Ecdh(kp2.PublicKey, kp1.PrivateKey, out var sec2));

                Assert.True(secp256k1.Ecdh(kp1.PublicKey, kp1.PrivateKey, out var sec3));

                Assert.Equal(sec1.AsSpan().ToHexString(), sec2.AsSpan().ToHexString());
                Assert.NotEqual(sec3.AsSpan().ToHexString(), sec2.AsSpan().ToHexString());
            }
        }

        [Fact]
        public void EcdhTestCustomHash()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var kp1 = GenerateKeyPair(secp256k1);
                var kp2 = GenerateKeyPair(secp256k1);

                EcdhHashFunction hashFunc = (Span<byte> output, Span<byte> x, Span<byte> y, IntPtr data) => 
                {
                    // XOR points together (dumb)
                    for (var i = 0; i < 32; i++)
                    {
                        output[i] = (byte)(x[i] ^ y[i]);
                    }
                    return 1;
                };

                Assert.True(secp256k1.Ecdh(kp1.PublicKey, kp2.PrivateKey, hashFunc, IntPtr.Zero, out var sec1));

                Assert.True(secp256k1.Ecdh(kp2.PublicKey, kp1.PrivateKey, hashFunc, IntPtr.Zero, out var sec2));

                Assert.True(secp256k1.Ecdh(kp1.PublicKey, kp1.PrivateKey, hashFunc, IntPtr.Zero, out var sec3));

                Assert.Equal(sec1.AsSpan().ToHexString(), sec2.AsSpan().ToHexString());
                Assert.NotEqual(sec3.AsSpan().ToHexString(), sec2.AsSpan().ToHexString());
            }
        }

        [Fact]
        public void KeyPairGeneration()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var kp = GenerateKeyPair(secp256k1);
            }
        }

        [Fact]
        public void SignAndVerify()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var kp = GenerateKeyPair(secp256k1);
                Span<byte> msg = new byte[32];
                RandomNumberGenerator.Create().GetBytes(msg);
                Assert.True(secp256k1.Sign(msg, kp.PrivateKey, out var signature));
                Assert.True(secp256k1.Verify(signature, msg, kp.PublicKey));
            }
        }

        [Fact]
        public void ParseDerSignatureTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                Span<byte> signatureOutput = new byte[Secp256k1.SIGNATURE_LENGTH];

                Span<byte> validDerSignature = "30440220484ECE2B365D2B2C2EAD34B518328BBFEF0F4409349EEEC9CB19837B5795A5F5022040C4F6901FE489F923C49D4104554FD08595EAF864137F87DADDD0E3619B0605".HexToBytes();                
                Assert.True(secp256k1.SignatureParseDer(validDerSignature, signatureOutput));

                Span<byte> invalidDerSignature = "00".HexToBytes();
                Assert.False(secp256k1.SignatureParseDer(invalidDerSignature, signatureOutput));
            }
        }

        /*
        [Fact]
        public void SignatureNormalize()
        {
            using (var secp256k1 = new Secp256k1())
            {
                Assert.True(secp256k1.SignatureNormalize()
            }
        }
        */

        [Fact]
        public void SigningTest()
        {
            using (var secp256k1 = new Secp256k1())
            {

                Span<byte> messageHash = new byte[] { 0xc9, 0xf1, 0xc7, 0x66, 0x85, 0x84, 0x5e, 0xa8, 0x1c, 0xac, 0x99, 0x25, 0xa7, 0x56, 0x58, 0x87, 0xb7, 0x77, 0x1b, 0x34, 0xb3, 0x5e, 0x64, 0x1c, 0xca, 0x85, 0xdb, 0x9f, 0xef, 0xd0, 0xe7, 0x1f };
                Span<byte> secretKey = "e815acba8fcf085a0b4141060c13b8017a08da37f2eb1d6a5416adbb621560ef".HexToBytes();

                bool result = secp256k1.SignRecoverable(messageHash, secretKey, out var signature);
                Assert.True(result);

                // Recover the public key
                result = secp256k1.Recover(signature, messageHash, out var publicKeyOutput);
                Assert.True(result);

                // Serialize the public key
                Span<byte> serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                result = secp256k1.PublicKeySerialize(publicKeyOutput, serializedKey);
                Assert.True(result);

                // Slice off any prefix.
                serializedKey = serializedKey.Slice(serializedKey.Length - Secp256k1.PUBKEY_LENGTH);

                Assert.Equal("0x3a2361270fb1bdd220a2fa0f187cc6f85079043a56fb6a968dfad7d7032b07b01213e80ecd4fb41f1500f94698b1117bc9f3335bde5efbb1330271afc6e85e92", serializedKey.ToHexString(), true);

                // Verify it works with variables generated from our managed code.
                BigInteger ecdsa_r = BigInteger.Parse("68932463183462156574914988273446447389145511361487771160486080715355143414637");
                BigInteger ecdsa_s = BigInteger.Parse("47416572686988136438359045243120473513988610648720291068939984598262749281683");
                byte recoveryId = 1;

                byte[] ecdsa_r_bytes = BigIntegerConverter.GetBytes(ecdsa_r);
                byte[] ecdsa_s_bytes = BigIntegerConverter.GetBytes(ecdsa_s);

                // Allocate memory for the signature and create a serialized-format signature to deserialize into our native format (platform dependent, hence why we do this).
                Span<byte> serializedSignature = ecdsa_r_bytes.Concat(ecdsa_s_bytes).ToArray();
                result = secp256k1.RecoverableSignatureParseCompact(serializedSignature, recoveryId, out var _);
                if (!result)
                    throw new Exception("Unmanaged EC library failed to parse serialized signature.");

                // Recover the public key
                result = secp256k1.Recover(signature, messageHash, out publicKeyOutput);
                Assert.True(result);


                // Serialize the public key
                serializedKey = new byte[Secp256k1.SERIALIZED_UNCOMPRESSED_PUBKEY_LENGTH];
                result = secp256k1.PublicKeySerialize(publicKeyOutput, serializedKey);
                Assert.True(result);

                // Slice off any prefix.
                serializedKey = serializedKey.Slice(serializedKey.Length - Secp256k1.PUBKEY_LENGTH);

                // Assert our key
                Assert.Equal("0x3a2361270fb1bdd220a2fa0f187cc6f85079043a56fb6a968dfad7d7032b07b01213e80ecd4fb41f1500f94698b1117bc9f3335bde5efbb1330271afc6e85e92", serializedKey.ToHexString(), true);
            }
        }

        [Fact]
        public void AdditionTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var kp1 = GenerateKeyPair(secp256k1);
                var tweak = GeneratePrivateKey(secp256k1);
                Assert.True(secp256k1.PrivateKeyTweakAdd(tweak, kp1.PrivateKey));
                Assert.True(secp256k1.PublicKeyCreate(kp1.PrivateKey, out var pkDerived));
                Assert.NotEqual(kp1.PublicKey.ToHexString(), pkDerived.AsSpan().ToHexString());

                Assert.True(secp256k1.PublicKeyTweakAdd(tweak, kp1.PublicKey));
                Assert.True(secp256k1.PublicKeyCreate(kp1.PrivateKey, out var pkFinal));
                Assert.Equal(kp1.PublicKey.ToHexString(), pkFinal.AsSpan().ToHexString());
            }
        }

        [Fact]
        public void MultiplicationTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var tweak = GeneratePrivateKey(secp256k1);
                var kp = GenerateKeyPair(secp256k1);
                Assert.True(secp256k1.PrivateKeyTweakMultiply(tweak, kp.PrivateKey));
                Assert.True(secp256k1.PublicKeyCreate(kp.PrivateKey, out var pkDerived));
                Assert.NotEqual(kp.PublicKey.ToHexString(), pkDerived.AsSpan().ToHexString());
                Assert.True(secp256k1.PublicKeyTweakMultiply(tweak, kp.PublicKey));
                Assert.True(secp256k1.PublicKeyCreate(kp.PrivateKey, out var pkFinal));
                Assert.Equal(kp.PublicKey.ToHexString(), pkFinal.AsSpan().ToHexString());
            }
        }

        [Fact]
        public void PublicKeyCombineTest()
        {
            using (var secp256k1 = new Secp256k1())
            {
                Span<byte> compressed1 = "0241cc121c419921942add6db6482fb36243faf83317c866d2a28d8c6d7089f7ba".HexToBytes();
                var compressed2 = "02e6642fd69bd211f93f7f1f36ca51a26a5290eb2dd1b0d8279a87bb0d480c8443".HexToBytes();
                var expectedSum = "0384526253c27c7aef56c7b71a5cd25bebb66dddda437826defc5b2568bde81f07".HexToBytes();
                Assert.True(secp256k1.PublicKeyCombine(compressed1, compressed2, out var actualSumUnCompressed12));
                Assert.True(secp256k1.PublicKeySerialize(actualSumUnCompressed12, Flags.SECP256K1_EC_COMPRESSED, out var actualSum12));
                Assert.True(secp256k1.PublicKeyCombine(compressed2, compressed1, out var actualSumUnCompressed21));
                Assert.True(secp256k1.PublicKeySerialize(actualSumUnCompressed21, Flags.SECP256K1_EC_COMPRESSED, out var actualSum21));
                Assert.Equal(actualSum12.AsSpan().ToHexString(), actualSum21.AsSpan().ToHexString());
                Assert.Equal(actualSum12.AsSpan().ToHexString(), expectedSum.AsSpan().ToHexString());
            }
        }

        [Fact]
        public void ShouldBeHomomorphicInAddition()
        {
            using (var secp256k1 = new Secp256k1())
            {
                var tweak = GeneratePrivateKey(secp256k1);
                var kp = GenerateKeyPair(secp256k1);
                secp256k1.PrivateKeyTweakAdd(tweak, kp.PrivateKey);
                Assert.True(secp256k1.PublicKeyCreate(tweak, out var tweakPubkey));
                Assert.True(secp256k1.PublicKeyCreate(kp.PrivateKey, out var finalPubKey1));
                secp256k1.PublicKeyCombine(tweakPubkey, kp.PublicKey, out var finalPubKey2);
                Assert.Equal(finalPubKey1.AsSpan().ToHexString(), finalPubKey2.AsSpan().ToHexString());
            }
        }
    }

    public static class Extensions
    {
        public static string ToHexString(this Span<byte> span)
        {
            return "0x" + BitConverter.ToString(span.ToArray()).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] HexToBytes(this string hexString)
        {
            int chars = hexString.Length;
            byte[] bytes = new byte[chars / 2];
            for (int i = 0; i < chars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return bytes;
        }
    }

    public abstract class BigIntegerConverter
    {
        /// <summary>
        /// Obtains the bytes that represent the BigInteger as if it was a big endian 256-bit integer.
        /// </summary>
        /// <param name="bigInteger">The BigInteger to obtain the byte representation of.</param>
        /// <returns>Returns the bytes that represent BigInteger as if it was a 256-bit integer.</returns>
        public static byte[] GetBytes(BigInteger bigInteger, int byteCount = 32)
        {
            // Obtain the bytes which represent this BigInteger.
            byte[] result = bigInteger.ToByteArray();

            // We'll operate on the data in little endian (since we'll extend the array anyways and we'd have to copy the data over anyways).
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            // Store the original size of the data, then resize it to the size of a word.
            int originalSize = result.Length;
            Array.Resize(ref result, byteCount);

            // BigInteger uses the most significant bit as sign and optimizes to return values like -1 as 0xFF instead of as 0xFFFF or larger (since there is no bound size, and negative values have all leading bits set)
            // Instead if we wanted to represent 256 (0xFF), we would add a leading zero byte so the sign bit comes from it, and will be zero (positive) (0x00FF), this way, BigInteger knows to represent this as a positive value.
            // Because we resized the array already, it would have added leading zero bytes which works for positive numbers, but if it's negative, all extended bits should be set, so we check for that case.

            // If the integer is negative, any extended bits should all be set.
            if (bigInteger.Sign < 0)
                for (int i = originalSize; i < result.Length; i++)
                    result[i] = 0xFF;

            // Flip the array so it is in big endian form.
            Array.Reverse(result);

            return result;
        }
    }
}