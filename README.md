# LicenceHub Enterprise — Software License Management System

> **A production-quality Enterprise Software License Governance & Optimization Platform built with .NET 10 C# Web API, PostgreSQL (Local), React 19 + Vite, Tailwind CSS, JWT Authentication, and OpenAI-powered license intelligence.**

---

## 🌟 Executive Overview & Key Features

* **Real-time License Governance**: Centralized tracking of software licenses, vendors, seat capacities, subscription cycles, and expenditures.
* **Atomic Transactional Operations**: Strict database transaction locking for seat allocations, revocations, and renewal processing.
* **Proactive Expiry & Renewal Pipeline**: Real-time categorization of license health (Critical <7d, Expiring <30d, Upcoming <90d, Active >90d).
* **Role-Based Access Control (RBAC)**: Enforced across both backend API controllers and frontend routes (`ADMIN`, `IT_MANAGER`, `EMPLOYEE`).
* **Audit Compliance Trail**: Immutable audit history logging for all administrative operations, authentication events, and data mutations.
* **AI-Powered Natural Language Assistant**: Live chat widget with controlled tool-calling execution (with action confirmation tokens) and offline semantic fallback.
* **Enterprise Reports**: 6 analytical report types (Utilization, Expiry, Employee Allocations, Cost Analysis, Renewals, Department Breakdown) with CSV and Print exports.
* **Local Development Setup**: 100% native execution on Windows/macOS/Linux without Docker.

---

## 🏗️ Architecture & Technology Stack

```
LicenceManagementSystem/
├── backend/
│   └── LicenseManagement.API/       # .NET 10 ASP.NET Core Web API
│       ├── Controllers/             # RESTful API Endpoints (12 Controllers)
│       ├── Data/                    # EF Core DbContext & DbInitializer (Seed Data)
│       ├── DTOs/                    # Data Transfer Objects
│       ├── Middleware/              # Global Exception Handler & Standard JSON Envelope
│       ├── Migrations/              # EF Core Migrations
│       ├── Models/                  # PostgreSQL Entities & Domain Models
│       ├── Services/                # Core Business Logic & DB Transactions
│       └── Validators/              # FluentValidation Request Rules
├── frontend/
│   └── license-management-ui/       # React 19 + Vite + Tailwind CSS
│       ├── src/
│       │   ├── components/          # Reusable UI & Layout Components
│       │   ├── context/             # AuthContext (JWT) & ToastContext
│       │   ├── pages/               # Enterprise Application Screens
│       │   └── services/            # Axios API Clients
├── tests/
│   └── LicenseManagement.Tests/     # xUnit Test Suite (11 Business Rule Tests)
├── .env.example                     # Environment Configuration Template
└── README.md                        # Documentation & Quickstart
```

### Technology Matrix
| Layer | Technologies |
|---|---|
| **Backend API** | .NET 10, C# 14, ASP.NET Core Web API, Entity Framework Core 10 |
| **Database** | PostgreSQL (Local Native Instance), Npgsql EF Core Provider |
| **Authentication** | JWT Bearer Tokens, BCrypt Password Hashing, RBAC |
| **Validation** | FluentValidation |
| **Frontend UI** | React 19, Vite, Tailwind CSS, Lucide Icons, Recharts |
| **AI Assistant** | OpenAI API (GPT-4o-mini tool calling) + Local Semantic Engine Fallback |
| **Testing** | xUnit, Moq, EF Core InMemory Provider |

---

## 📋 Business Rules Enforcement

| Rule | Business Logic Description | Backend Enforcement | Frontend Enforcement |
|---|---|---|---|
| **Rule 1** | A license **cannot** be assigned if `AvailableCount <= 0` | `AssignmentService.cs` checks and throws `InvalidOperationException` | UI disables assignment button with warning badge |
| **Rule 2** | A license **cannot** be assigned if expired (`ExpiryDate < DateTime.UtcNow`) | `AssignmentService.cs` validates date and throws error | UI marks license as Expired and disables selection |
| **Rule 3** | Inactive employees **cannot** receive new licenses | `AssignmentService.cs` verifies `Employee.Status == "Active"` | Modal filters out or blocks inactive employee selection |
| **Rule 4** | An employee **cannot** have duplicate active assignments of same license | `AssignmentService.cs` verifies unique active composite key | UI blocks re-selection with duplicate notice |
| **Rule 5** | Assigning a license increments `AssignedCount` and decrements `AvailableCount` | `AssignmentService.cs` updates in atomic DB transaction | Instant real-time UI state recalculation |
| **Rule 6** | Revoking a license restores 1 seat to `AvailableCount` | `AssignmentService.cs` decrements `AssignedCount`, increments `AvailableCount` | Instant real-time UI state recalculation |
| **Rule 7** | `AvailableCount` can never drop below `0` | Database check & transactional constraint | Input validation |
| **Rule 8** | `AssignedCount` can never exceed `TotalLicenses` | Database check & validation constraint | Max capacity barrier |
| **Rule 9** | Only `ADMIN` can permanently delete licenses (if no active assignments) | `LicensesController.cs` `[Authorize(Roles = "ADMIN")]` & active check | Button hidden from non-admin roles |
| **Rule 10** | Renewal `NewExpiryDate` must be strictly after current `ExpiryDate` | `RenewalService.cs` validates dates & resets status to `Active` | Date picker minimum restriction & error toast |

---

## 🚀 Local Development Setup (NO DOCKER)

### 1. Prerequisites
Ensure you have the following installed on your machine:
* **.NET 10 SDK** (`dotnet --version` >= 10.0)
* **Node.js** (`node -v` >= 18.0) and **npm** (`npm -v` >= 9.0)
* **PostgreSQL** installed locally and running on port `5432`

---

### 2. PostgreSQL Database Setup
1. Open your PostgreSQL client (pgAdmin, psql, or DBeaver).
2. Create a database named `LicenseManagementDb`:
   ```sql
   CREATE DATABASE "LicenseManagementDb";
   ```
3. Update connection string in `backend/LicenseManagement.API/appsettings.json` or `.env` file:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=LicenseManagementDb;Username=postgres;Password=your_password"
   }
   ```

---

### 3. Backend API Startup
1. Open a terminal and navigate to the backend directory:
   ```bash
   cd backend/LicenseManagement.API
   ```
2. Apply database migrations:
   ```bash
   dotnet ef database update
   ```
3. Run the ASP.NET Core API server:
   ```bash
   dotnet run
   ```
4. The API will start on:
   * **API Base URL**: `http://localhost:5000` or `https://localhost:5001`
   * **Swagger OpenAPI Docs**: `http://localhost:5000/swagger`

> **Note on Automatic Seeding**: On the initial run, `DbInitializer` will automatically seed demo employees, enterprise licenses, initial assignments, audit records, and user credentials.

---

### 4. Frontend UI Startup
1. Open a new terminal and navigate to the frontend directory:
   ```bash
   cd frontend/license-management-ui
   ```
2. Install frontend dependencies:
   ```bash
   npm install
   ```
3. Start the Vite development server:
   ```bash
   npm run dev
   ```
4. Open your browser and navigate to: **`http://localhost:5173`**

---

## 🔑 Demo User Accounts & Credentials

The system comes pre-configured with 3 role tiers. The login screen also features **1-click quick-fill buttons** for fast evaluation:

| Role | Email | Password | Permissions |
|---|---|---|---|
| **Admin / CIO** | `admin@enterprise.com` *(or `kritika@enterprise.com`)* | `Admin@123` | **Full System Administration**: Employee CRUD, License CRUD, Allocation Workflow, Renewals, Reports, Audit History Logs, User Management (Kritika Chouhan, CIO). |
| **IT Manager** | `priyanka@enterprise.com` | `ItManager@123` | **License Operations**: View Employees, License Inventory Management, Assign/Revoke Seats, Process Renewals, View & Export Reports. |
| **Employee** | `rahul@enterprise.com` | `Employee@123` | **Self-Service Portal**: View Personal Assigned Licenses, Contract Expiry Countdown, License Inventory Catalog, Account Profile. |

---

## 🤖 AI Assistant Capabilities & Tool Calling

The application includes an embedded AI Chatbot (`AiChatWidget.jsx`) with two operating modes:

1. **Online Mode (with OpenAI API Key)**:
   * Configure `OPENAI_API_KEY` in environment variables.
   * Model: `gpt-4o-mini` with structured JSON tool-calling across 13 backend data functions.
   * Proposes mutations (License assignment, revocation, renewal) and provides a **live confirmation token card**.
   * Clicking "Confirm & Execute" in the UI sends the action token to `/api/ai/action/confirm` to execute the database transaction with full audit logging.
2. **Offline Semantic Mode (No API Key Required)**:
   * If no API key is provided, `AiService.cs` uses a built-in semantic intent parser.
   * Queries real PostgreSQL database metrics (`GetLicenseSummary`, `GetAvailableLicenses`, `GetExpiringLicenses`, etc.) and returns formatted Markdown answers with accurate numbers.

---

## 🧪 Automated Testing & Quality Assurance

Run the automated xUnit test suite covering all business rules, assignment constraints, renewal validations, and password hashing:

```bash
# Run unit & integration tests
dotnet test tests/LicenseManagement.Tests/LicenseManagement.Tests.csproj

# Verify frontend production build
cd frontend/license-management-ui
npm run build
```

---

## 📡 REST API Summary

| Endpoint | Method | Role Required | Description |
|---|---|---|---|
| `/api/auth/login` | `POST` | Anonymous | Authenticates user and returns JWT Bearer token |
| `/api/profile` | `GET` / `PUT` | Authenticated | Retrieves/updates logged-in user profile & password |
| `/api/dashboard/overview` | `GET` | Authenticated | Aggregated KPIs, utilization charts, and AI heuristics |
| `/api/employees` | `GET` / `POST` | `ADMIN`, `IT_MANAGER` | Employee list with filters & creation |
| `/api/employees/{id}` | `GET` / `PUT` / `DELETE` | `ADMIN` / `IT_MANAGER` | Employee details, update, and soft/hard delete |
| `/api/licenses` | `GET` / `POST` | Authenticated / `ADMIN` | License inventory list & registration |
| `/api/licenses/{id}` | `GET` / `PUT` / `DELETE` | `ADMIN` / `IT_MANAGER` | License details, update, and deletion (Admin only) |
| `/api/assignments` | `GET` / `POST` | `ADMIN`, `IT_MANAGER` | List assignments and execute seat allocation |
| `/api/assignments/{id}/revoke` | `POST` | `ADMIN`, `IT_MANAGER` | Revoke license and restore available seat |
| `/api/my-licenses` | `GET` | Authenticated | Employee self-service personal license list |
| `/api/renewals` | `GET` / `POST` | `ADMIN`, `IT_MANAGER` | Renewal history log & execute license renewal |
| `/api/reports/{type}` | `GET` | `ADMIN`, `IT_MANAGER` | 6 report analytical queries |
| `/api/reports/export/csv` | `GET` | `ADMIN`, `IT_MANAGER` | Direct CSV export download |
| `/api/audit` | `GET` | `ADMIN` | Immutable compliance audit history log |
| `/api/ai/chat` | `POST` | Authenticated | Natural language query with tool calling |
| `/api/ai/action/confirm` | `POST` | `ADMIN`, `IT_MANAGER` | Executes confirmed AI proposed action token |
| `/api/notifications` | `GET` / `POST` | Authenticated | Real-time expiry alerts & notification stream |

---

## 🔒 Security & Compliance

* **256-bit BCrypt Password Hashing**: Passwords are never stored in plaintext.
* **JWT Bearer Claims**: Role and identity claims validated on every API request.
* **Database Concurrency & Transactions**: `BeginTransactionAsync` used for all seat allocations and revocations to prevent race conditions.
* **Input Sanitization & Validation**: FluentValidation rules for emails, employee codes, seat counts, and dates.
* **CORS Whitelisting**: Strict origin controls for frontend application.

---

**Built with pride for Enterprise Software Asset Governance.**
