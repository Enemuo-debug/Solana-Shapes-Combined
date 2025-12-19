const mongoose = require('mongoose');

const WalletSchema = new mongoose.Schema({
  publicKey: { type: String, required: true, unique: true },
  encryptedPrivateKey: { type: String, required: true },
  iv: { type: String, required: true },

  status: {
    type: String,
    enum: ['available', 'assigned'],
    default: 'available',
    index: true
  },

  assignedAt: {
    type: Date,
    default: null
  }

}, { timestamps: true });

module.exports = mongoose.model('Wallet', WalletSchema);
