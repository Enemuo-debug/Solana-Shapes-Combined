const express = require('express');
const router = express.Router();
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const { validateWalletAddress } = require("../services/solanaServices")

const User = require('../models/User');

router.post('/register', async (req, res) => {
  try {
    const { name, walletAddress } = req.body;
    console.log(name, walletAddress);

    let isValidAddress = await validateWalletAddress(walletAddress);
    console.log(name, walletAddress, isValidAddress);
    if (!isValidAddress) {
      return res.status(400).json({ error: 'Invalid wallet address (Please use a valid solana wallet address)' });
    }

    if (!name || !walletAddress) {
      return res.status(400).json({ error: 'name and walletAddress are required' });
    }

    const salt = await bcrypt.genSalt(10);

    const user = new User({ name, walletAddress });
    await user.save();

    return res.status(201).json({ message: 'User created' });
  } catch (err) {
    console.error(err);
    return res.status(500).json({ error: 'Server error' });
  }
});

router.post('/login', async (req, res) => {
  try {
    const { name, walletAddress } = req.body;
    if (!name || !walletAddress) return res.status(400).json({ error: 'name and wallet required' });

    const user = await User.findOne({ name, walletAddress });
    if (!user) return res.status(401).json({ error: 'Invalid credentials' });

    const token = jwt.sign({ id: user._id, name: user.name }, process.env.JWT_SECRET || 'dev_secret', {
      expiresIn: '7d'
    });

    return res.json({ token });
  } catch (err) {
    console.error(err);
    return res.status(500).json({ error: 'Server error' });
  }
});

module.exports = router;