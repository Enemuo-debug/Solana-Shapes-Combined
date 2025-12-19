const jwt = require('jsonwebtoken');
const User = require('../models/User');

const auth = async (req, res, next) => {
  try {
    const header = req.headers.authorization;
    if (!header) return res.status(401).json({ error: 'Missing Authorization header' });

    const parts = header.split(' ');
    if (parts.length !== 2 || parts[0] !== 'Bearer')
      return res.status(401).json({ error: 'Invalid Authorization format' });

    const token = parts[1];
    const payload = jwt.verify(token, process.env.JWT_SECRET || 'dev_secret');

    const user = await User.findById(payload.id);
    if (!user) return res.status(401).json({ error: 'User not found' });

    req.user = user;
    next();
  } catch (err) {
    return res.status(401).json({ error: 'Invalid or expired token' });
  }
};

module.exports = auth;
