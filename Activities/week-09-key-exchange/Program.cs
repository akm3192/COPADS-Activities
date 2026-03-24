using System.Security.Cryptography;
using System.Text;

Console.WriteLine("=== Week 9: Key Exchange ===");
Console.WriteLine("Public Key Cryptography & Diffie-Hellman\n");

Console.WriteLine(@"
Last week's problem: How do you share a secret key securely?

If Alice and Bob want to communicate:
- They need a shared secret key for AES encryption
- But they can't send the key over the network (Eve might intercept it!)
- They've never met in person to exchange keys

Solution: Public Key Cryptography!
");

// ============================================
// PART 1: The Key Distribution Problem
// ============================================
Console.WriteLine("--- Part 1: The Key Distribution Problem ---\n");

Console.WriteLine(@"
The Chicken-and-Egg Problem:
┌─────────────────────────────────────────────────────────┐
│                                                         │
│   Alice ─────── Insecure Channel ─────── Bob           │
│     │                  │                  │            │
│     │           Eve (listening)           │            │
│     │                  │                  │            │
│     └──── How to share secret key? ──────┘            │
│                                                         │
└─────────────────────────────────────────────────────────┘

1976: Whitfield Diffie and Martin Hellman publish their solution!
");

// ============================================
// PART 2: Asymmetric vs Symmetric
// ============================================
Console.WriteLine("--- Part 2: Asymmetric vs Symmetric ---\n");

Console.WriteLine(@"
SYMMETRIC (AES):          ASYMMETRIC (RSA):
- Same key for both       - Two different keys
- Fast                    - Slower (1000x)
- Key distribution hard   - Key distribution easy!

Asymmetric keys come in pairs:
┌─────────────────────────────────────────────────┐
│  PUBLIC KEY              PRIVATE KEY            │
│  - Share with everyone   - Keep secret!         │
│  - Used to ENCRYPT       - Used to DECRYPT      │
│  - Cannot decrypt        - Cannot encrypt       │
└─────────────────────────────────────────────────┘

Real-world analogy: A padlock!
- Public key = open padlock (give to anyone)
- Private key = the key to that padlock (keep secret)
");

// ============================================
// PART 3: RSA in Action
// ============================================
Console.WriteLine("--- Part 3: RSA in Action ---\n");

Console.WriteLine("Generating RSA key pair (this is computationally expensive)...\n");

using var rsa = RSA.Create(2048);

// Export keys
var publicKey = rsa.ExportRSAPublicKey();
var privateKey = rsa.ExportRSAPrivateKey();

Console.WriteLine($"Public key size:  {publicKey.Length} bytes");
Console.WriteLine($"Private key size: {privateKey.Length} bytes\n");

// Alice sends a message to Bob using Bob's public key
string aliceMessage = "Hello Bob! This is a secret message.";
byte[] messageBytes = Encoding.UTF8.GetBytes(aliceMessage);

Console.WriteLine($"Alice's message: \"{aliceMessage}\"\n");

// Encrypt with public key
byte[] encrypted = rsa.Encrypt(messageBytes, RSAEncryptionPadding.OaepSHA256);
Console.WriteLine($"Encrypted (first 50 hex): {Convert.ToHexString(encrypted)[..50]}...");
Console.WriteLine($"Encrypted size: {encrypted.Length} bytes\n");

// Decrypt with private key
byte[] decrypted = rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
string bobReceived = Encoding.UTF8.GetString(decrypted);

Console.WriteLine($"Bob decrypts: \"{bobReceived}\"");
Console.WriteLine($"Success: {aliceMessage == bobReceived}\n");

// ============================================
// PART 4: Diffie-Hellman Key Exchange
// ============================================
Console.WriteLine("--- Part 4: Diffie-Hellman Key Exchange ---\n");

Console.WriteLine(@"
Diffie-Hellman lets two parties create a SHARED SECRET
even though all their communication is public!

The Math (simplified):
1. Alice and Bob agree on public values: p (prime), g (generator)
2. Alice picks secret 'a', computes A = g^a mod p, sends A
3. Bob picks secret 'b', computes B = g^b mod p, sends B
4. Alice computes: s = B^a mod p
5. Bob computes:   s = A^b mod p
6. MAGIC: Both get the same value s!

Eve sees: p, g, A, B
Eve cannot compute: s (without solving 'discrete logarithm' - very hard!)
");

// Manual DH exercise
Console.WriteLine(@"
TASK 3.1: Manual Diffie-Hellman (p=23, g=5)
============================================
Work with another team to exchange keys manually!

1. Pick a secret number 'a' between 2 and 20:    a = _____
2. Compute A = 5^a mod 23:                       A = _____
   (Hint: Use calculator or: Math.Pow(5, a) % 23)
3. Exchange A values with another team
   Send your A, receive their B:                 B = _____
4. Compute shared secret s = B^a mod 23:         s = _____
5. Verify with other team - do your secrets match? _____

Example calculation (a=6):
  A = 5^6 mod 23 = 15625 mod 23 = 8
  If B=19: s = 19^6 mod 23 = 47045881 mod 23 = 2
");

// Quick calculator for manual DH
Console.WriteLine("Quick calculator for Task 3.1:");
int p = 23, g = 5;
int exampleA = 6; // Students should pick their own
long A = (long)Math.Pow(g, exampleA) % p;
Console.WriteLine($"  If a={exampleA}: A = {g}^{exampleA} mod {p} = {A}");
Console.WriteLine($"  If B=19, a={exampleA}: s = 19^{exampleA} mod {p} = {(long)Math.Pow(19, exampleA) % p}\n");

Console.WriteLine("TASK 3.2: ECDH Demo (Modern Diffie-Hellman)\n");

// Modern DH uses Elliptic Curves (ECDH)
using var alice = ECDiffieHellman.Create();
using var bob = ECDiffieHellman.Create();

// Public keys can be shared openly
byte[] alicePublic = alice.PublicKey.ExportSubjectPublicKeyInfo();
byte[] bobPublic = bob.PublicKey.ExportSubjectPublicKeyInfo();

Console.WriteLine($"Alice's public key: {Convert.ToHexString(alicePublic)[..40]}...");
Console.WriteLine($"Bob's public key:   {Convert.ToHexString(bobPublic)[..40]}...\n");

// Each derives the same shared secret!
using var bobKeyForAlice = ECDiffieHellman.Create();
bobKeyForAlice.ImportSubjectPublicKeyInfo(bobPublic, out _);
byte[] aliceShared = alice.DeriveKeyMaterial(bobKeyForAlice.PublicKey);

using var aliceKeyForBob = ECDiffieHellman.Create();
aliceKeyForBob.ImportSubjectPublicKeyInfo(alicePublic, out _);
byte[] bobShared = bob.DeriveKeyMaterial(aliceKeyForBob.PublicKey);

Console.WriteLine($"Alice's derived secret: {Convert.ToHexString(aliceShared)}");
Console.WriteLine($"Bob's derived secret:   {Convert.ToHexString(bobShared)}");
Console.WriteLine($"Secrets match: {aliceShared.SequenceEqual(bobShared)}\n");

Console.WriteLine("Now Alice and Bob can use this shared secret as an AES key!");

// ============================================
// PART 5: The Hybrid Approach (How TLS Works)
// ============================================
Console.WriteLine("\n--- Part 5: The Hybrid Approach ---\n");

Console.WriteLine(@"
Real systems combine asymmetric and symmetric:

1. Use Diffie-Hellman to exchange a session key
2. Use AES with that session key for actual data
3. Best of both worlds: secure key exchange + fast encryption

This is exactly how HTTPS/TLS works!

┌─────────────────────────────────────────────────────────┐
│ TLS Handshake (simplified):                            │
│                                                         │
│ Client ────────── Server                               │
│   │ ClientHello (supported ciphers)   │                │
│   │ ─────────────────────────────────>│                │
│   │                                    │                │
│   │ ServerHello + Certificate + DH    │                │
│   │ <─────────────────────────────────│                │
│   │                                    │                │
│   │ Client DH + Finished              │                │
│   │ ─────────────────────────────────>│                │
│   │                                    │                │
│   │   [Now both have shared secret]   │                │
│   │   [Switch to AES encryption]      │                │
└─────────────────────────────────────────────────────────┘
");

// ============================================
// PART 6: YOUR TASK
// ============================================
Console.WriteLine("--- Part 6: YOUR TASK ---\n");

Console.WriteLine(@"
Implement a simple secure messaging system:

Task 1: Complete the SecureChannel class
----------------------------------------
- Perform DH key exchange
- Derive an AES key from the shared secret
- Encrypt/decrypt messages using AES

Task 2: Demonstrate the exchange
--------------------------------
- Create two SecureChannel instances (Alice and Bob)
- Exchange public keys
- Send encrypted messages back and forth
- Show that Eve (with only public info) cannot decrypt

Task 3: Digital Signatures (Bonus)
----------------------------------
- Use RSA to SIGN a message (encrypt with PRIVATE key)
- Verify signature (decrypt with PUBLIC key)
- This proves the message came from the claimed sender

Look for the TODO comments in the SecureChannel class!
");

// ============================================
// PART 7: Discussion Questions
// ============================================
Console.WriteLine("--- Part 7: Discussion Questions ---\n");

Console.WriteLine(@"
1. Why do we use ECDH instead of RSA for key exchange in modern TLS?

   _______________________________________________________________

2. What is 'forward secrecy' and why is it important?

   _______________________________________________________________

3. Why can't Eve compute the shared secret from the public values?

   _______________________________________________________________

4. What's the purpose of the Certificate in TLS?

   _______________________________________________________________

5. How does a digital signature differ from encryption?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• Asymmetric crypto solves the key distribution problem
• RSA: Encrypt with public key, decrypt with private key
• Diffie-Hellman: Create shared secret over public channel
• Hybrid approach: DH for key exchange, AES for data
• TLS/HTTPS uses this combination to secure the web
• Digital signatures prove authenticity (encrypt with private key)
");

// ============================================
// SecureChannel Class (for students to complete)
// ============================================

public class SecureChannel
{
    private readonly ECDiffieHellman _ecdh;
    private byte[]? _sharedKey;

    public byte[] PublicKey { get; }

    public SecureChannel()
    {
        _ecdh = ECDiffieHellman.Create();
        PublicKey = _ecdh.PublicKey.ExportSubjectPublicKeyInfo();
    }

    // TODO: Complete this method
    public void DeriveSharedKey(byte[] otherPartyPublicKey)
    {
        // 1. Import the other party's public key
        // 2. Derive the shared key material
        // 3. Store it in _sharedKey (use first 32 bytes for AES-256)

        throw new NotImplementedException("TODO: Implement DeriveSharedKey");
    }

    // TODO: Complete this method
    public byte[] Encrypt(string message)
    {
        if (_sharedKey == null)
            throw new InvalidOperationException("Key not yet derived");

        // 1. Generate random IV
        // 2. Encrypt message with AES using _sharedKey
        // 3. Return IV + ciphertext concatenated

        return Array.Empty<byte>();  // TODO: Implement
    }

    // TODO: Complete this method
    public string Decrypt(byte[] encryptedData)
    {
        if (_sharedKey == null)
            throw new InvalidOperationException("Key not yet derived");

        // 1. Extract IV (first 16 bytes)
        // 2. Extract ciphertext (remaining bytes)
        // 3. Decrypt with AES using _sharedKey
        // 4. Return plaintext string

        return "";  // TODO: Implement
    }
}
