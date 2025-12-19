const express = require('express');
const router = express.Router();
const { nanoid } = require('nanoid');
const { createWallet, verifyTransaction } = require('../services/solanaServices');
const { encryptPrivateKey } = require('../services/cryptoService');
const auth = require('../middleware/auth');
const Poll = require('../models/Poll');
const User = require('../models/User');
const { CashOutPolls } = require('../services/cashOutPolls');
const Wallet = require("../models/Wallet")

router.post('/', auth, async (req, res) => {
  try {
    const { title } = req.body;
    if (!title) {
      return res.status(400).json({ error: 'title is required' });
    }

    let joinCode = nanoid(6).toUpperCase();
    while (await Poll.findOne({ joinCode })) {
      joinCode = nanoid(6).toUpperCase();
    }

    let wallet = await Wallet.findOneAndUpdate(
      { status: 'available' },
      { status: 'assigned', assignedAt: new Date() },
      { new: true }
    );

    if (!wallet) {
      const { publicKey, secretKey } = createWallet();

      const secretKeyBase64 = Buffer.from(secretKey).toString("base64");

      const { encryptedPrivateKey, iv } = encryptPrivateKey(secretKeyBase64);

      wallet = await Wallet.create({
        publicKey,
        encryptedPrivateKey,
        iv,
        status: 'assigned',
        assignedAt: new Date()
      });
    }

    const poll = new Poll({
      title,
      createdBy: req.user._id,
      joinCode,
      participants: [],
      participantScores: [],
      wallet: {
        publicKey: wallet.publicKey,
        encryptedPrivateKey: wallet.encryptedPrivateKey,
        iv: wallet.iv
      },
      usedTransactions: []
    });

    await poll.save();

    return res.status(201).json({
      id: poll._id,
      title: poll.title,
      joinCode: poll.joinCode,
      walletAddress: poll.wallet.publicKey
    });

  } catch (err) {
    console.error(err);
    return res.status(500).json({ error: 'Server error' });
  }
});

router.post('/mine/:joinCode', auth, async (req, res) => {
  try {
    const joinCode = req.params.joinCode.toUpperCase();
    const { transactionVerification } = req.body;

    console.log(transactionVerification);

    const poll = await Poll.findOne({ joinCode });
    if (!poll) return res.status(404).json({ error: 'Poll not found' });
    
    try {
      // Verify the transaction on Solana blockchain for 0.01 SOL
      const verificationResult = await verifyTransaction(transactionVerification, poll.wallet.publicKey);
      
      // Check if transaction has already been used
      if (poll.usedTransactions && poll.usedTransactions.includes(transactionVerification)) {
        return res.status(400).json({ error: 'This transaction has already been used' });
      }

      if(poll.participants.includes(req.user._id)) return res.status(400).json({ error: 'Already joined' });
      
      poll.participants.push(req.user._id);
      poll.participantScores.push(0);
      
      // Record this transaction as used to prevent reuse
      if (!poll.usedTransactions) {
        poll.usedTransactions = [];
      }
      poll.usedTransactions.push(transactionVerification);
      
      await poll.save();
      return res.json({ 
        token: 'Transaction verified',
        amountReceived: verificationResult.amountSol,
        transactionSignature: verificationResult.signature
      });
    } catch (err) {
      console.error('Transaction verification failed:', err);
      return res.status(400).json({ error: 'Transaction verification failed: ' + err.message });
    }
    } catch (err) {
    console.error(err);
    return res.status(500).json({ error: 'Server error' });
  }
});

router.post('/:joinCode/join', auth, async (req, res) => {
  try {
    const joinCode = req.params.joinCode.toUpperCase();
    const { transactionVerification } = req.body;
    console.log(transactionVerification);

    if (!transactionVerification) {
      return res.status(400).json({ error: 'Transaction signature required' });
    }

    const poll = await Poll.findOne({ joinCode });
    if (!poll) {
      return res.status(404).json({ error: 'Poll not found' });
    }

    if (
      poll.usedTransactions &&
      poll.usedTransactions.includes(transactionVerification)
    ) {
      return res.status(400).json({
        error: 'This transaction has already been used'
      });
    }

    if (poll.participants.includes(req.user._id)) {
      return res.status(400).json({ error: 'Already joined' });
    }

    const verificationResult = await verifyTransaction(
      transactionVerification,
      poll.wallet.publicKey
    );

    poll.participants.push(req.user._id);
    poll.participantScores.push(0);

    if (!poll.usedTransactions) {
      poll.usedTransactions = [];
    }
    poll.usedTransactions.push(transactionVerification);

    await poll.save();

    return res.json({
      message: 'Joined poll successfully',
      amountReceived: verificationResult.amountSol,
      transactionSignature: verificationResult.signature
    });

  } catch (err) {
    console.error('Join failed:', err);
    return res.status(400).json({
      error: 'Join failed: ' + err.message
    });
  }
});

router.post("/update", auth, async (req, res) => {
  try {
    const { score } = req.body;

    if (score === undefined || score === null) {
      return res.status(400).json({ error: 'Score is required' });
    }

    if (typeof score !== 'number' || score < 0) {
      return res.status(400).json({ error: 'Score must be a non-negative number' });
    }

    const polls = await Poll.find({ participants: req.user._id });

    if (polls.length === 0) {
      return res.status(404).json({ error: 'You are not a participant in any polls' });
    }

    const updateResults = [];
    
    for (const poll of polls) {
      const participantIndex = poll.participants.findIndex(
        id => id.toString() === req.user._id.toString()
      );

      if (participantIndex !== -1) {
        poll.participantScores[participantIndex] += score;
        await poll.save();

        updateResults.push({
          pollId: poll._id,
          pollTitle: poll.title,
          joinCode: poll.joinCode,
          newScore: poll.participantScores[participantIndex]
        });
      }
    }

    return res.json({
      message: 'Scores updated successfully',
      updatedPolls: updateResults.length,
      results: updateResults
    });

  } catch (err) {
    console.error('Error updating scores:', err);
    return res.status(500).json({ error: 'Server error' });
  }
});

router.get('/me/list', auth, async (req, res) => {
  try {
    await CashOutPolls();
    const polls = await Poll.find({ participants: req.user._id }).select('title joinCode wallet.publicKey').sort({ createdAt: -1 });
    return res.json(polls);
  } catch (err) {
    console.error(err);
    return res.status(500).json({ error: 'Server error' });
  }
});

module.exports = router;