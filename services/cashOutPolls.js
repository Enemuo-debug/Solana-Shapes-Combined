import Poll from "../models/Poll.js";
import User from "../models/User.js";
import solanaWeb3 from "@solana/web3.js";
import { connection, verifyTransaction } from "./solanaServices.js";
import { decryptPrivateKey } from "./cryptoService.js";
import Wallet from "../models/Wallet.js"

function computeRankedWinners(poll) {
  if (
    !poll.participants ||
    !poll.participantScores ||
    poll.participants.length !== poll.participantScores.length
  ) {
    return [];
  }

  return poll.participants
    .map((userId, index) => ({
      userId,
      score: poll.participantScores[index]
    }))
    .sort((a, b) => b.score - a.score);
}

async function sendAllSOL(fromSecretBase64, toAddress) {
  const secretKey = Uint8Array.from(
    Buffer.from(fromSecretBase64, "base64")
  );
  console.log("Secret key byte length:", secretKey.length);
  const fromWallet = solanaWeb3.Keypair.fromSecretKey(secretKey);
  const toPubkey = new solanaWeb3.PublicKey(toAddress);

  const balance = await connection.getBalance(fromWallet.publicKey);

  if (balance <= 0) {
    throw new Error("Poll wallet is empty");
  }

  const txForFee = new solanaWeb3.Transaction().add(
    solanaWeb3.SystemProgram.transfer({
      fromPubkey: fromWallet.publicKey,
      toPubkey,
      lamports: balance
    })
  );

  txForFee.feePayer = fromWallet.publicKey;
  txForFee.recentBlockhash = (
    await connection.getLatestBlockhash()
  ).blockhash;

  const fee = await txForFee.getEstimatedFee(connection);

  if (!fee || balance <= fee) {
    throw new Error("Insufficient balance after fee");
  }

  const lamportsToSend = balance - fee;

  const finalTx = new solanaWeb3.Transaction().add(
    solanaWeb3.SystemProgram.transfer({
      fromPubkey: fromWallet.publicKey,
      toPubkey,
      lamports: lamportsToSend
    })
  );

  const signature = await solanaWeb3.sendAndConfirmTransaction(
    connection,
    finalTx,
    [fromWallet]
  );

  return signature;
}

export async function CashOutPolls() {
  const now = new Date();

  const polls = await Poll.find({
    $expr: {
      $gte: [
        { $subtract: [now, "$createdAt"] },
        { $multiply: ["$duration", 1440 * 60 * 1000] }
      ]
    }
  });

  if (polls.length === 0) return;

  for (const poll of polls) {
    console.log(`Processing poll ${poll._id}`);

    if (poll.participants.length === 0) {
      console.log(`Poll ${poll._id} has no participants`);

      await Wallet.updateOne(
        { publicKey: poll.wallet.publicKey },
        { status: 'available', assignedAt: null }
      );

      await Poll.findByIdAndDelete(poll._id);
      continue;
    }

    let decryptedPrivateKey;
    try {
      decryptedPrivateKey = decryptPrivateKey(
        poll.wallet.encryptedPrivateKey,
        poll.wallet.iv
      );
    } catch (err) {
      console.error("Wallet decryption failed:", err.message);
      continue;
    }

    const rankedWinners = computeRankedWinners(poll);

    if (rankedWinners.length === 0) {
      console.warn(`No valid participants for poll ${poll._id}`);
      continue;
    }
    
    for (const rank of rankedWinners) {
      try {
        const user = await User.findById(rank.userId);

        if (!user || !user.walletAddress) {
          throw new Error("Invalid winner wallet");
        }

        console.log(`Trying payout → ${user.walletAddress}`);

        const signature = await sendAllSOL(
          decryptedPrivateKey,
          user.walletAddress
        );

        await verifyTransaction(signature, user.walletAddress);

        console.log(`Poll ${poll._id} paid successfully`);

        // Release wallet back to pool
        await Wallet.updateOne(
          { publicKey: poll.wallet.publicKey },
          { status: 'available', assignedAt: null }
        );

        await Poll.findByIdAndDelete(poll._id);
        break;

      } catch (err) {
        console.error(
          `Payout failed for ${rank.userId}:`,
          err.message
        );
      }
    }
  }
}
