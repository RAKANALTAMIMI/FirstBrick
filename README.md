# 💼 Investment Platform — Microservices Backend

> Internship Project | Built with ASP.NET Core · JWT Auth · PostgreSQL · RabbitMQ · REST APIs

A microservices-based investment platform backend. Users can register, manage their wallets, browse and create investment projects, and track their portfolio — all through secure, token-authenticated REST APIs. Services communicate asynchronously via **RabbitMQ** for wallet provisioning and investment settlement.

---

## 📐 Architecture Overview

The system is split into three independent microservices, each owning its own domain, data, and responsibilities. They communicate through **REST** for synchronous operations and **RabbitMQ** for asynchronous event-driven flows.

```
┌──────────────────────────────────────────────────────────────┐
│                    API Gateway / Client                      │
└──────────────┬──────────────────┬──────────────────┬─────────┘
               │ REST             │ REST             │ REST
               ▼                  ▼                  ▼
   ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
   │   Account Service  │  │ Investment Service │  │  Payment Service   │
   │   (Auth + Users)   │  │ (Projects + Invest)│  │ (Wallet + Topup)  │
   └─────────┬──────────┘  └─────────┬──────────┘  └─────────┬──────────┘
             │ publish                │ publish ▲             │ publish ▲
             │                        ▼         │ consume      ▼         │ consume
             │            ┌────────────────────────────────────────────┐
             └───────────▶│                  RabbitMQ                  │◀───────────┘
                          │                                            │
                          │  user.created.q                            │
                          │  investment.user.created.q                 │
                          │  payment.investment.created.q              │
                          └────────────────────────────────────────────┘
             │                        │                     │
             ▼                        ▼                     ▼
   ┌────────────────────┐  ┌────────────────────┐  ┌────────────────────┐
   │     users table    │  │   projects table   │  │    wallets table   │
   │                    │  │ investments table  │  │ transactions table │
   └────────────────────┘  └────────────────────┘  └────────────────────┘
```

> Each service has its own database schema. JWT tokens issued by **Account Service** are validated across all services. RabbitMQ handles all cross-service side effects without tight coupling.

---

## 🧩 Services

### 1. 🔐 Account Service

Handles user identity — registration, login, and profile management. Issues **JWT tokens** used to authenticate requests across the entire platform.

**Base Route:** `/v1`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/v1/user` | ❌ | Register a new user |
| `POST` | `/v1/login` | ❌ | Login and receive JWT token |
| `GET` | `/v1/user/{user_id}` | ✅ | Get a user's profile |
| `PUT` | `/v1/user/{user_id}` | ✅ | Update a user's profile |

**Key Design Decisions:**
- Passwords are stored hashed (via `passwordb` field abstraction)
- JWT token is returned on successful login and must be passed as a `Bearer` token for protected routes
- `ModelState` validation guards all write endpoints

---

### 2. 📈 Investment Service

The core domain service. Lets users create investment projects and invest in others. All endpoints require authentication. Portfolio data is scoped to the authenticated user via JWT claims.

**Base Route:** `/v1`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/v1/project` | ✅ | Create a new investment project |
| `GET` | `/v1/projects` | ✅ | List all available projects |
| `POST` | `/v1/invest` | ✅ | Invest in a project |
| `GET` | `/v1/portfolio` | ✅ | Get the authenticated user's portfolio |
| `GET` | `/v1/portfolio/project/{projectid}` | ✅ | Get a specific project in user's portfolio |

**Key Design Decisions:**
- `[Authorize]` applied at the controller level — all routes are protected
- User identity extracted directly from JWT claims (`ClaimTypes.NameIdentifier`) — no user ID needed in request body
- `DbExceptionFilter` service filter handles database errors globally at controller scope
- Service layer returns a `response` object with a `Success` flag for clean controller logic

---

### 3. 💳 Payment Service

Manages user wallets and financial transactions. Handles fund top-ups (simulated via Apple Pay) and provides transaction history with pagination.

**Base Route:** `/v1`

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/v1/ApplepayTopup/{amount}` | ✅ | Add funds to the authenticated user's wallet |
| `GET` | `/v1/balance/{userId}` | ✅ | Get wallet balance for a user |
| `GET` | `/v1/transactions/{userId}/{page}` | ✅ | Paginated transaction history |

**Key Design Decisions:**
- Top-up atomically updates the wallet balance AND creates a transaction record
- `TransactionType` is an enum, ensuring only valid types (e.g., `Topup`) are recorded
- Amount validation (`> 0`) happens at the controller before any service calls
- Pagination supported with `page` and `pageSize` parameters

---

## 🐇 RabbitMQ — Event-Driven Flows

Services communicate asynchronously through RabbitMQ for operations that cross service boundaries. This keeps services fully decoupled — no service calls another directly.

### Queues

| Queue | Type | Publisher | Consumer |
|-------|------|-----------|----------|
| `user.created.q` | Classic | Account Service | Payment Service |
| `investment.user.created.q` | Classic | Account Service | Investment Service |
| `payment.investment.created.q` | Classic | Payment Service | Investment Service |

---

### Flow 1 — User Registration → Wallet Creation

When a new user registers, Account Service publishes an event. Payment Service consumes it and automatically provisions an empty wallet for that user.

```
Client
  │
  └─▶  POST /v1/user  ──▶  Account Service
                                │
                                │  registers user in DB
                                │
                                └──▶ publish ──▶ user.created.q
                                                      │
                                                      ▼
                                              Payment Service
                                                      │
                                                      └──▶ creates wallet (balance = 0)
                                                           for the new user
```

**Event payload on `user.created.q`:**
```json
{
  "userId": 42,
  "username": "john_doe",
  "fullname": "John Doe"
}
```

---

### Flow 2 — Investment Created → Payment Validation → Status Update

When a user submits an investment, it is saved with status `Pending`. Investment Service publishes the event to Payment Service, which checks if the wallet has sufficient funds. Payment Service then settles the transaction and publishes the result back. Investment Service consumes that result and updates the investment status to `Paid` or `Failed`.

```
Client
  │
  └─▶  POST /v1/invest  ──▶  Investment Service
                                    │
                                    │  saves investment (status = "Pending")
                                    │
                                    └──▶ publish ──▶ investment.user.created.q
                                                            │
                                                            ▼
                                                    Payment Service
                                                            │
                                                ┌───────────┴───────────┐
                                                │  Check wallet balance  │
                                                └───────────┬───────────┘
                                                            │
                                          ┌─────────────────┴─────────────────┐
                                     sufficient?                         insufficient?
                                          │                                    │
                                          ▼                                    ▼
                                   deduct amount                        no deduction
                                   create Topup TSX                     log failure
                                          │                                    │
                                          └──────────────┬────────────────────┘
                                                         │
                                              publish ──▶ payment.investment.created.q
                                              { status: "Paid" | "Failed" }
                                                         │
                                                         ▼
                                               Investment Service
                                                         │
                                                         └──▶ updates investment status
                                                              to "Paid" or "Failed"
```

**Event payload on `investment.user.created.q`:**
```json
{
  "investmentId": 7,
  "userId": 42,
  "projectId": 3,
  "amount": 500.00
}
```

**Event payload on `payment.investment.created.q`:**
```json
{
  "investmentId": 7,
  "status": "Paid"
}
```

> **Why this design?** Investment Service never calls Payment Service directly. This means either service can be down, restarted, or scaled independently without breaking the flow — RabbitMQ holds the message until the consumer is ready.

> See [`/docs/sequnce.png`] for the full ERD diagram.

---

## 🗄️ Database Schema

```sql
-- Account Service
users          (userid, username, passwordb, fullname, createdat)

-- Investment Service
projects       (projectid, ownerid, title, description, targetamount, fundedamount, createdat)
investments    (investmentid, userid, projectid, amount, status, createdat)

-- Payment Service
wallets        (id, user_id, balance, created_at)
transactions   (id, user_id, transaction_type, amount, created_at)
```

> See [`/docs/erd.png`] for the full ERD diagram.

---

## 🔒 Authentication Flow

```
1. Client  →  POST /v1/login  →  Account Service
2. Account Service validates credentials
3. Account Service  →  returns JWT token
4. Client stores token
5. Client  →  Any protected route  →  Bearer <token>
6. Service validates JWT, extracts user claims
7. Response returned
```

All protected services share the same JWT secret and validate tokens locally — no round-trip to the Account Service required.

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core (C#) |
| Auth | JWT Bearer Tokens |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| Messaging | RabbitMQ |
| Architecture | Microservices (REST + Event-Driven) |
| API Style | RESTful JSON APIs |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL running locally or via Docker
- RabbitMQ running locally or via Docker (`localhost:5672`)
- An HTTP client (Postman, curl, etc.)

### Running a Service

```bash
# Clone the repository
git clone https://github.com/your-username/investment-platform.git
cd investment-platform

# Navigate to a service
cd AccountService

# Set up your connection string in appsettings.json
# Then run:
dotnet restore
dotnet run
```

Repeat for `InvestmentService` and `PaymentService`.

### Environment Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=your_db;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "investment-platform",
    "Audience": "investment-platform-users"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

---

## 📁 Project Structure

```
investment-platform/
│
├── AccountService/
│   ├── Controllers/       # UsersController
│   ├── Models/            # User, LoginModel
│   ├── Utility/           # UserService, JWT helpers
│   └── appsettings.json
│
├── InvestmentService/
│   ├── Controllers/       # InvestmentController
│   ├── Models/            # Project, Investment
│   ├── Utility/           # InvestmentServicex, DbExceptionFilter
│   └── appsettings.json
│
├── PaymentService/
│   ├── Controllers/       # PaymentController
│   ├── Models/            # Wallet, Transaction, TransactionType
│   ├── Utility/           # PaymentServicex, DbExceptionFilter
│   └── appsettings.json
│
└── docs/
    └── erd.png
```

---

## 📬 Sample API Requests

**Register a user:**
```http
POST /v1/user
Content-Type: application/json

{
  "username": "john_doe",
  "passwordb": "securepassword",
  "fullname": "John Doe"
}
```

**Login:**
```http
POST /v1/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "securepassword"
}
```

**Invest in a project (authenticated):**
```http
POST /v1/invest
Authorization: Bearer <your_token>
Content-Type: application/json

{
  "projectid": 3,
  "amount": 500.00
}
```

**Top up wallet:**
```http
POST /v1/ApplepayTopup/1000
Authorization: Bearer <your_token>
```

---

## 🔮 Future Improvements

- [ ] API Gateway (e.g., YARP or Ocelot) to unify service entry points
- [ ] Docker Compose setup for one-command local dev
- [ ] Swagger/OpenAPI documentation per service
- [ ] Refresh token support in Account Service
- [ ] Dead-letter queues (DLQ) for failed RabbitMQ message handling
- [ ] Email notifications on investment settlement
- [ ] Admin dashboard for monitoring investment statuses

---

## 👨‍💻 Author
By **Rakan Altamimi**

Built during my internship as a backend engineering project.  
Feel free to reach out or open an issue if you have questions!

---
