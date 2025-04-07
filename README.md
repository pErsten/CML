# 🪙 Crypto Trading Backend API

This project is a simple mockup of a cryptocurrency trading platform. It supports account management, order placement, wallet tracking, and real-time updates via SignalR for a Blazor WebAssembly frontend.

---

## ✨ Features

- 🔐 JWT Authentication for users
- 📦 Calculate prices for EUR and BTC exchanges
- 📈 Real-time Bitcoin rate broadcasting
- 🧠 Background services for order book and rate fetching
- 🔁 SignalR hub for live data to the Blazor client

---

## 🧰 Tech Stack

- **.NET 8** — Web API + SignalR
- **Entity Framework Core** — SQL Server as DB
- **SignalR** — Real-time communication with frontend
- **JWT** — Authentication & Authorization
- **C# Channels** — For async order processing
- **Background Services** — Long-running rate & order tasks

---

## 🚀 Getting Started

### 1. Clone the repository
To run the full app, you'll need to start **two projects** from this repository:

1. **Backend API** (`CryptoTrading.Backend`)
2. **Frontend Blazor Client** (`CryptoTrading.Frontend`)

Follow the steps below to get everything up and running.

---

### 📦 Step 1: Database Setup

1. Create a new **SQL Server database** (e.g., `CryptoTradingDb`)
2. Update the connection string in `appsettings.json` under:

```json
{
  "Databases": {
    "SqlConnection": "[your path to mssql db]"
  }
}
```


Run ```Script-Migration``` to generate the SQL script to apply the scheme.

### 🧠 Step 2: Launch Projects
Make sure both projects are set as startup projects in your solution (you can do this in Visual Studio or manually):

✅ Start Backend API (CryptoTrading.Backend)

✅ Start Blazor Frontend (CryptoTrading.Frontend)

Once both are running:

The backend should be available at https://localhost:7057.

The frontend Blazor app should open in your browser, typically at https://localhost:7121.

🧪 Step 3: Test the App
Use Swagger (/swagger) in the backend to manually test login, register, and order endpoints.

Open the Blazor client to test the UI and SignalR-based real-time updates.

✅ You’re all set!

### Screenshots:

- Main screen:
![image](https://github.com/user-attachments/assets/e638c07c-2bc8-48a5-b041-85c7e252230f)
- Exchange calculator:
![image](https://github.com/user-attachments/assets/f032f0e8-3de1-487e-a6db-e556e8f33c67)
- Selecting order book snapshot from list:
![image](https://github.com/user-attachments/assets/f9cdaa7c-81b7-4f1b-b205-b85d45a15d0d)
- Login form:
![image](https://github.com/user-attachments/assets/bf095a6c-e467-4f97-9cbd-2f9e01b180c9)
- Swagger:
![image](https://github.com/user-attachments/assets/8a82e3fb-2494-4dbd-999c-1ed6d38c517b)

