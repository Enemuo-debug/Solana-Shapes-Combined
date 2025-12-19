const crypto = require("crypto");
require("dotenv").config();

const ENCRYPTION_KEY = crypto
  .createHash("sha256")
  .update(process.env.WALLET_SECRET)
  .digest(); // 32 bytes

function encryptPrivateKey(secretKeyBase64) {
  // 🚨 Guardrail
  const secretBytes = Buffer.from(secretKeyBase64, "base64");
  if (secretBytes.length !== 64) {
    throw new Error("Invalid Solana secret key length at encryption");
  }

  const iv = crypto.randomBytes(16);
  const cipher = crypto.createCipheriv("aes-256-cbc", ENCRYPTION_KEY, iv);

  const encryptedBytes = Buffer.concat([
    cipher.update(secretBytes),
    cipher.final()
  ]);

  return {
    encryptedPrivateKey: encryptedBytes.toString("base64"),
    iv: iv.toString("base64")
  };
}

function decryptPrivateKey(encryptedBase64, ivBase64) {
  const iv = Buffer.from(ivBase64, "base64");
  const encryptedBytes = Buffer.from(encryptedBase64, "base64");

  const decipher = crypto.createDecipheriv(
    "aes-256-cbc",
    ENCRYPTION_KEY,
    iv
  );

  const decryptedBytes = Buffer.concat([
    decipher.update(encryptedBytes),
    decipher.final()
  ]);

  // 🚨 Guardrail
  if (decryptedBytes.length !== 64) {
    throw new Error("Decrypted secret key is corrupted");
  }

  // Return base64 for Solana
  return decryptedBytes.toString("base64");
}

// function test() {
//   // Example string (any length)
//   const testString = "Hello World";

//   // Convert string to bytes
//   const testBytes = Buffer.from(testString, "utf-8");

//   // Pad or trim to 64 bytes for testing encryption
//   const paddedBytes = Buffer.alloc(64);
//   testBytes.copy(paddedBytes);

//   // Convert back to base64 so encryptPrivateKey accepts it
//   const fakeSecretKey = paddedBytes.toString("base64");

//   // Encrypt and decrypt
//   const cipher = encryptPrivateKey(fakeSecretKey);
//   const decipher = decryptPrivateKey(cipher.encryptedPrivateKey, cipher.iv);

//   // Trim any null bytes from decryption
//   const decryptedString = Buffer.from(decipher, "base64").toString("utf-8").replace(/\0/g, "");

//   console.log("Original:", testString);
//   console.log("Decrypted:", decryptedString);
//   console.log("Match:", testString === decryptedString);
// }

// test();


module.exports = { encryptPrivateKey, decryptPrivateKey };