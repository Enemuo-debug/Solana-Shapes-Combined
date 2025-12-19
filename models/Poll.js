const mongoose = require('mongoose');

const PollSchema = new mongoose.Schema({
  title: { type: String, required: true, unique: true, trim: true },
  wallet: {
    publicKey: { type: String, required: true },    
    encryptedPrivateKey: { type: String, required: true },  
    iv: { type: String, required: true }  
  },
  usedTransactions: [{ type: String }],
  joinCode: { type: String, required: true, index: true, unique: true },
  duration: { type: Number, required: true, default: 1 },
  participants: [{ type: mongoose.Schema.Types.ObjectId, ref: 'User' }],
  participantScores: [{ type: Number }],
  createdAt: { type: Date, default: Date.now }
});

module.exports = mongoose.model('Poll', PollSchema);