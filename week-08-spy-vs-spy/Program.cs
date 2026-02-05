using System.Security.Cryptography;
using System.Text;

Console.WriteLine("=== Week 8: Spy vs Spy ===");
Console.WriteLine("Introduction to Cryptography\n");

Console.WriteLine(@"
Welcome, Agent! Your mission: Learn to protect secrets.

In network communication, anyone can intercept your messages.
Cryptography lets us communicate securely even over insecure channels.

Today we'll explore:
1. Symmetric encryption (same key to encrypt/decrypt)
2. Hashing (one-way transformation)
3. Why we need asymmetric encryption (next week!)
");

// ============================================
// PART 1: The Problem - Plaintext is Dangerous
// ============================================
Console.WriteLine("--- Part 1: The Problem ---\n");

string secretMessage = "Meet me at the old warehouse at midnight";
byte[] messageBytes = Encoding.UTF8.GetBytes(secretMessage);

Console.WriteLine($"Original message: \"{secretMessage}\"");
Console.WriteLine($"As bytes (hex):   {Convert.ToHexString(messageBytes)}");
Console.WriteLine();
Console.WriteLine("Anyone who intercepts this can read it!");
Console.WriteLine("We need to encrypt it.\n");

// ============================================
// PART 2: Symmetric Encryption (AES)
// ============================================
Console.WriteLine("--- Part 2: Symmetric Encryption (AES) ---\n");

Console.WriteLine(@"
AES (Advanced Encryption Standard):
- Same key encrypts AND decrypts
- Very fast - used for bulk data
- Problem: How do you share the key securely?

    Sender                          Receiver
    ──────                          ────────
    plaintext ──┐                ┌── plaintext
                │                │
            [ENCRYPT]        [DECRYPT]
                │                │
                └── ciphertext ──┘
                       │
                   SHARED KEY
");

// Generate a random key and IV
byte[] key = new byte[32];  // 256 bits for AES-256
byte[] iv = new byte[16];   // 128-bit IV
RandomNumberGenerator.Fill(key);
RandomNumberGenerator.Fill(iv);

Console.WriteLine($"Key (256-bit): {Convert.ToHexString(key)}");
Console.WriteLine($"IV  (128-bit): {Convert.ToHexString(iv)}\n");

// Encrypt
byte[] encrypted = EncryptAES(messageBytes, key, iv);
Console.WriteLine($"Encrypted (hex): {Convert.ToHexString(encrypted)}");
Console.WriteLine($"Encrypted (garbled): {Encoding.UTF8.GetString(encrypted)[..20]}...\n");

// Decrypt
byte[] decrypted = DecryptAES(encrypted, key, iv);
string decryptedMessage = Encoding.UTF8.GetString(decrypted);
Console.WriteLine($"Decrypted: \"{decryptedMessage}\"");
Console.WriteLine($"Match: {decryptedMessage == secretMessage}\n");

// ============================================
// PART 3: What Happens with Wrong Key?
// ============================================
Console.WriteLine("--- Part 3: Wrong Key = Garbage ---\n");

byte[] wrongKey = new byte[32];
RandomNumberGenerator.Fill(wrongKey);

try
{
    byte[] badDecrypt = DecryptAES(encrypted, wrongKey, iv);
    Console.WriteLine($"With wrong key: {Encoding.UTF8.GetString(badDecrypt)}");
}
catch (CryptographicException ex)
{
    Console.WriteLine($"With wrong key: FAILED - {ex.Message}");
}
Console.WriteLine("Without the correct key, the message is unrecoverable.\n");

// ============================================
// PART 4: Hashing - One-Way Functions
// ============================================
Console.WriteLine("--- Part 4: Hashing ---\n");

Console.WriteLine(@"
Hashing is different from encryption:
- One-way: Cannot recover original from hash
- Deterministic: Same input = same output
- Fixed size: Any input produces same-length hash
- Avalanche effect: Small change = completely different hash

Uses: Password storage, data integrity, digital signatures
");

string password = "MySecretPassword123";
string password2 = "MySecretPassword124";  // One character different

byte[] hash1 = SHA256.HashData(Encoding.UTF8.GetBytes(password));
byte[] hash2 = SHA256.HashData(Encoding.UTF8.GetBytes(password2));

Console.WriteLine($"Password 1: {password}");
Console.WriteLine($"SHA-256:    {Convert.ToHexString(hash1)}\n");

Console.WriteLine($"Password 2: {password2}");
Console.WriteLine($"SHA-256:    {Convert.ToHexString(hash2)}\n");

Console.WriteLine("Notice: One character change = completely different hash!");
Console.WriteLine($"Hashes match: {hash1.SequenceEqual(hash2)}\n");

// ============================================
// PART 5: YOUR TASK
// ============================================
Console.WriteLine("--- Part 5: YOUR TASK ---\n");

Console.WriteLine(@"
Task 1: Implement a simple Caesar cipher
----------------------------------------
The Caesar cipher shifts each letter by a fixed amount.
'HELLO' with shift 3 becomes 'KHOOR'

Complete the CaesarEncrypt and CaesarDecrypt methods below.

Task 2: Crack the Caesar cipher!
--------------------------------
Given an encrypted message, figure out the shift amount.
Hint: Try all 26 possible shifts and look for readable text.

Task 3: Password verification
-----------------------------
Implement VerifyPassword that checks if a password matches
a stored hash WITHOUT storing the actual password.

Look for the TODO comments below!
");

// Test Caesar cipher
Console.WriteLine("Testing Caesar cipher implementation:\n");

string testPlain = "HELLO WORLD";
int shift = 3;
string testEncrypted = CaesarEncrypt(testPlain, shift);
string testDecrypted = CaesarDecrypt(testEncrypted, shift);

Console.WriteLine($"Original:  {testPlain}");
Console.WriteLine($"Encrypted: {testEncrypted}");
Console.WriteLine($"Decrypted: {testDecrypted}");
Console.WriteLine($"Success:   {testPlain == testDecrypted}\n");

// Crack this!
string intercepted = "WKLV LV D VHFUHW PHVVDJH";
Console.WriteLine($"Intercepted message: {intercepted}");
Console.WriteLine("Can you crack it? (Hint: try CrackCaesar method)\n");

// ============================================
// PART 6: Discussion Questions
// ============================================
Console.WriteLine("--- Part 6: Discussion Questions ---\n");

Console.WriteLine(@"
1. Why can't we use symmetric encryption alone for internet communication?

   _______________________________________________________________

2. Why is hashing used for passwords instead of encryption?

   _______________________________________________________________

3. Why do we need an IV (Initialization Vector) for AES?

   _______________________________________________________________

4. What makes the Caesar cipher so easy to break?

   _______________________________________________________________

5. How does the 'avalanche effect' help with security?

   _______________________________________________________________
");

// ============================================
// Summary
// ============================================
Console.WriteLine("\n=== Key Takeaways ===");
Console.WriteLine(@"
• Symmetric encryption: Fast, but key distribution is hard
• AES is the modern standard for symmetric encryption
• Hashing is one-way - cannot recover original
• SHA-256 is commonly used for integrity and passwords
• Classical ciphers (Caesar) are easily broken
• Next week: Public key crypto solves the key distribution problem!
");

// ============================================
// Crypto Helper Methods
// ============================================

byte[] EncryptAES(byte[] plaintext, byte[] aesKey, byte[] aesIV)
{
    using var aes = Aes.Create();
    aes.Key = aesKey;
    aes.IV = aesIV;

    using var encryptor = aes.CreateEncryptor();
    return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
}

byte[] DecryptAES(byte[] ciphertext, byte[] aesKey, byte[] aesIV)
{
    using var aes = Aes.Create();
    aes.Key = aesKey;
    aes.IV = aesIV;

    using var decryptor = aes.CreateDecryptor();
    return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
}

// TODO: Implement Caesar cipher encryption
string CaesarEncrypt(string plaintext, int shiftAmount)
{
    // Shift each letter by shiftAmount positions
    // Only shift A-Z (uppercase), leave other characters unchanged
    // 'A' shifted by 3 = 'D', 'Z' shifted by 3 = 'C' (wraps around)

    var result = new StringBuilder();

    foreach (char c in plaintext)
    {
        if (c >= 'A' && c <= 'Z')
        {
            // TODO: Implement the shift logic
            result.Append(c);  // Replace this with shifted character
        }
        else
        {
            result.Append(c);  // Keep non-letters unchanged
        }
    }

    return result.ToString();
}

// TODO: Implement Caesar cipher decryption
string CaesarDecrypt(string ciphertext, int shiftAmount)
{
    // Hint: Decryption is just encryption with negative shift
    // Or shift by (26 - shiftAmount)

    return ciphertext;  // TODO: Implement this
}

// TODO: Implement Caesar cipher cracker
string CrackCaesar(string ciphertext)
{
    // Try all 26 possible shifts
    // Return the one that looks like readable English
    // Hint: Look for common words like "THE", "AND", "IS"

    return ciphertext;  // TODO: Implement this
}

// TODO: Implement password verification
bool VerifyPassword(string inputPassword, byte[] storedHash)
{
    // Hash the input password and compare to stored hash
    // Return true if they match

    return false;  // TODO: Implement this
}
