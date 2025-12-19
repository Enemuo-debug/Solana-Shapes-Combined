const solanaWeb3 = require('@solana/web3.js');

const connection = new solanaWeb3.Connection(
  solanaWeb3.clusterApiUrl('mainnet-beta'),
  'confirmed'
);

function createWallet() {
  const wallet = solanaWeb3.Keypair.generate();

  console.log(wallet.publicKey.toBase58(), wallet.secretKey)

  return {
    publicKey: wallet.publicKey.toBase58(),
    secretKey: wallet.secretKey
  };
}

async function getBalance(publicKey) {
  try {
    const balanceLamports = await connection.getBalance(
      new solanaWeb3.PublicKey(publicKey)
    );
    return balanceLamports / solanaWeb3.LAMPORTS_PER_SOL;
  } catch (error) {
    console.error('Error getting balance:', error);
    throw new Error('Failed to get wallet balance');
  }
}

async function sendSOL(fromSecretBase64, toAddress, amountSol) {
  try {
    const secretKey = Uint8Array.from(Buffer.from(fromSecretBase64, 'base64'));
    const fromWallet = solanaWeb3.Keypair.fromSecretKey(secretKey);

    const transaction = new solanaWeb3.Transaction().add(
      solanaWeb3.SystemProgram.transfer({
        fromPubkey: fromWallet.publicKey,
        toPubkey: new solanaWeb3.PublicKey(toAddress),
        lamports: amountSol * solanaWeb3.LAMPORTS_PER_SOL
      })
    );

    const signature = await solanaWeb3.sendAndConfirmTransaction(
      connection,
      transaction,
      [fromWallet]
    );

    return signature;
  } catch (error) {
    console.error('Error sending SOL:', error);
    throw new Error('Failed to send SOL: ' + error.message);
  }
}

async function verifyTransaction(txSignature, expectedReceiver) {
  try {
    // Validate input
    if (!txSignature || typeof txSignature !== 'string') {
      throw new Error('Invalid transaction signature');
    }

    if (!expectedReceiver || typeof expectedReceiver !== 'string') {
      throw new Error('Invalid receiver address');
    }

    // Fetch transaction with retries
    let tx = null;
    let retries = 3;
    
    while (retries > 0 && !tx) {
      try {
        tx = await connection.getTransaction(txSignature, {
          commitment: 'confirmed',
          maxSupportedTransactionVersion: 0
        });
        
        if (!tx) {
          retries--;
          if (retries > 0) {
            await new Promise(resolve => setTimeout(resolve, 2000));
          }
        }
      } catch (fetchError) {
        console.error('Error fetching transaction (retries left: ' + retries + '):', fetchError);
        retries--;
        if (retries > 0) {
          await new Promise(resolve => setTimeout(resolve, 2000));
        }
      }
    }

    if (!tx) {
      throw new Error('Transaction not found or not yet confirmed on the blockchain');
    }

    // Check if transaction failed
    if (tx.meta && tx.meta.err !== null) {
      throw new Error('Transaction failed on the blockchain');
    }

    // Get account keys (handle both legacy and versioned transactions)
    let accountKeys;
    if (tx.transaction.message.getAccountKeys) {
      // Versioned transaction
      accountKeys = tx.transaction.message.getAccountKeys().staticAccountKeys.map(k => k.toBase58());
    } else {
      // Legacy transaction
      accountKeys = tx.transaction.message.accountKeys.map(k => k.toBase58());
    }

    console.log('Transaction account keys:', accountKeys);
    console.log('Expected receiver:', expectedReceiver);

    // Find receiver in account keys
    const receiverIndex = accountKeys.findIndex(
      key => key === expectedReceiver
    );

    if (receiverIndex === -1) {
      throw new Error('Receiver wallet not found in transaction - payment was sent to wrong address');
    }

    // Calculate amount received
    const preBalance = tx.meta.preBalances[receiverIndex];
    const postBalance = tx.meta.postBalances[receiverIndex];
    const lamportsReceived = postBalance - preBalance;
    const amountSol = lamportsReceived / solanaWeb3.LAMPORTS_PER_SOL;

    console.log('Transaction verification:', {
      signature: txSignature,
      receiver: expectedReceiver,
      preBalance,
      postBalance,
      lamportsReceived,
      amountSol
    });

    // Validate minimum amount
    const MIN_JOIN_AMOUNT = Number(process.env.JOIN_AMT);

    if (amountSol < MIN_JOIN_AMOUNT) {
      throw new Error(`Insufficient payment: ${amountSol.toFixed(4)} SOL (minimum ${MIN_JOIN_AMOUNT} SOL required)`);
    }

    return {
      valid: true,
      signature: txSignature,
      to: expectedReceiver,
      amountSol: parseFloat(amountSol.toFixed(4)),
      timestamp: tx.blockTime
    };

  } catch (error) {
    console.error('Transaction verification error:', error);
    throw error;
  }
}

async function getTransactionStatus(txSignature) {
  try {
    const statuses = await connection.getSignatureStatuses([txSignature]);
    
    if (!statuses || !statuses.value || !statuses.value[0]) {
      return {
        found: false,
        confirmed: false
      };
    }

    const status = statuses.value[0];
    
    return {
      found: true,
      confirmed: status.confirmationStatus === 'confirmed' || status.confirmationStatus === 'finalized',
      confirmationStatus: status.confirmationStatus,
      slot: status.slot,
      err: status.err
    };
  } catch (error) {
    console.error('Error getting transaction status:', error);
    throw new Error('Failed to get transaction status');
  }
}

async function validateWalletAddress(publicKey) {
  try
  {
    new solanaWeb3.PublicKey(publicKey);
    return true;
  } catch {
    return false;
  }
}

async function sendAllSOL(fromSecretBase64, toAddress) {
  try {
    const secretKey = Uint8Array.from(
      Buffer.from(fromSecretBase64, "base64")
    );

    const fromWallet = solanaWeb3.Keypair.fromSecretKey(secretKey);
    const toPubkey = new solanaWeb3.PublicKey(toAddress);

    // 1. Get wallet balance
    const balance = await connection.getBalance(fromWallet.publicKey);

    if (balance <= 0) {
      throw new Error("Wallet has zero balance");
    }

    // 2. Build a dummy transaction to estimate fee
    const transaction = new solanaWeb3.Transaction().add(
      solanaWeb3.SystemProgram.transfer({
        fromPubkey: fromWallet.publicKey,
        toPubkey,
        lamports: balance
      })
    );

    transaction.feePayer = fromWallet.publicKey;
    const { blockhash } = await connection.getLatestBlockhash();
    transaction.recentBlockhash = blockhash;

    const fee = await transaction.getEstimatedFee(connection);

    if (!fee) {
      throw new Error("Failed to estimate transaction fee");
    }

    const lamportsToSend = balance - fee;

    if (lamportsToSend <= 0) {
      throw new Error("Insufficient balance after fee deduction");
    }

    // 3. Rebuild transaction with correct amount
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

    return {
      signature,
      lamportsSent: lamportsToSend,
      solSent: lamportsToSend / solanaWeb3.LAMPORTS_PER_SOL
    };

  } catch (err) {
    console.error("sendAllSOL error:", err);
    throw err;
  }
}

module.exports = { 
  connection, 
  validateWalletAddress,
  createWallet, 
  getBalance, 
  sendSOL, 
  verifyTransaction,
  getTransactionStatus,
  sendAllSOL
};