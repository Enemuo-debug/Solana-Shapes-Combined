# Solana Shapes Combined

## Project Overview

Solana Shapes Combined is a unified repository containing both the backend server and the Unity game client for the Solana Shapes project. This project was built as part of the Solana University Fall Hackathon and demonstrates a Web3-powered gaming architecture where real Solana wallets back competitive gameplay and poll-based interactions.

The system is designed with a strong emphasis on security, fairness, and on-chain transparency. All sensitive wallet operations are handled server-side, ensuring private keys are never exposed to the client.

---

## Demo

A full video demonstration of the project can be viewed here:

https://drive.google.com/file/d/1g9QBOWbsY0xqmVmor565l2BBfE-jF3zw/view?usp=drive_link

---

## Repository Structure


---

## Backend (Node.js + Express)

The backend is responsible for user management, poll orchestration, secure Solana wallet handling, and reward distribution.

### Backend Features

- User registration and authentication using wallet addresses and unique usernames
- Poll creation for competitive gameplay
- Dedicated Solana wallet generation per poll
- Server-side encryption and storage of private keys
- Automated cash-out of poll rewards to winners after a defined interval

### Backend Technologies

- Node.js
- Express
- MongoDB
- Solana Web3.js
- AES-256-CBC encryption

All cryptographic operations occur exclusively on the server.

---

## Unity Game Client

The Unity game client serves as the interactive gameplay layer.

### Gameplay Mechanics

- Players match incoming shapes and colours
- Survival time directly affects score
- The game ends when the hit count reaches zero
- Player scores are synchronized with all active polls

The Unity client never handles private keys and communicates only through secure backend endpoints.

---

## Installation and Usage

### Backend Setup

1. Navigate to the backend directory: `cd backend`
2. Install dependencies:
3. Create a `.env` file and configure required environment variables
4. Start the server: `npm start`

---

### Unity Game Setup

1. Open the project inside the `game` directory using Unity
2. Ensure the correct Unity version and platform settings are configured
3. Build and run the project from the Unity Editor

---

## Security Design

- Private keys are never exposed to the client
- All wallet secret keys are encrypted before storage
- Strict validation ensures only valid Solana secret keys are processed
- Wallet decryption occurs only during transaction signing

---

## Open Source

This project is fully open source and includes both backend and game client code for transparency and extensibility.
