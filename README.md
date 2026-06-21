# 🛒 Enterprise E-Commerce Management System

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET Core](https://img.shields.io/badge/.NET%208.0-Purple?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop%20App-blue)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-green)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-orange)
![License](https://img.shields.io/badge/License-MIT-yellow)

A comprehensive, enterprise-grade E-Commerce Management Application built on the Windows Presentation Foundation (WPF) framework. This project strictly adheres to the **MVVM (Model-View-ViewModel)** architectural pattern, emphasizing clean separation of concerns, testability, and high maintainability.

## 📋 Table of Contents
- [System Architecture](#-system-architecture)
- [Core Features](#-core-features)
- [Technology Stack](#-technology-stack)
- [Getting Started](#-getting-started)
- [Contributing](#-contributing)

---

## 🏛 System Architecture

The application is engineered using modern software design principles (SOLID) and incorporates several established design patterns:

- **MVVM Pattern**: Total decoupling of the UI (XAML) from business logic and data state.
- **Event-Driven Communication**: Utilizing a centralized `MessageBus` for loosely coupled inter-component messaging (e.g., cross-ViewModel event triggers without direct references).
- **Service Locator / Dependency Injection**: Centralized services for domain logic (e.g., `CartService`, authentication states).
- **Code-First Database Design**: Leveraging Entity Framework Core migrations to maintain synchronization between domain models and the SQL Server schema.

---

## 🚀 Core Features

The system implements Role-Based Access Control (RBAC) with three distinct operational modules:

### 👤 1. Customer Portal (Buyer)
- **Product Catalog**: Advanced search, filtering, and detailed product views.
- **Cart & Wishlist Engine**: Persistent state management for shopping carts and user wishlists.
- **Micro-interactions (UX)**: Incorporates smooth UI transitions, custom hover effects, dynamic fly-to-cart animations, and non-blocking Toast Notifications.

### 🏪 2. Vendor Dashboard (Seller)
- **Analytics & Reporting**: Real-time visualization of sales metrics and order volumes.
- **Inventory Management**: Full CRUD operations for product catalogs, pricing, and stock levels.
- **Order Processing Pipeline**: Workflow management for order fulfillment, shipping, and return requests.

### 👑 3. System Administration (Admin)
- **Identity & Access Management**: Centralized user control, role assignments, and account suspension.
- **Vendor Onboarding**: Verification and approval workflows for new merchant accounts.
- **Marketing & Promotions**: Management of global campaigns, category routing, and discount vouchers.

---

## 🛠 Technology Stack

- **Runtime & Language**: .NET 8.0, C# 12
- **Presentation Layer**: WPF (Windows Presentation Foundation)
- **UI Component Library**: `MaterialDesignInXamlToolkit`
- **MVVM Framework**: `CommunityToolkit.Mvvm`
- **Data Access Layer (ORM)**: Entity Framework Core 8.0
- **Database Engine**: Microsoft SQL Server / LocalDB

---

## ⚙️ Getting Started

### Prerequisites
- Visual Studio 2022 (v17.8+ with `.NET Desktop Development` workload)
- .NET 8.0 SDK
- SQL Server 2019+ or LocalDB

### Installation & Deployment

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/TMDT.git
   cd TMDT
   ```

2. **Open the Solution**:
   Launch `TMDT.sln` using Visual Studio 2022. The IDE will automatically restore required NuGet packages.

3. **Initialize the Database**:
   Navigate to `Tools` > `NuGet Package Manager` > `Package Manager Console` and execute:
   ```powershell
   Update-Database
   ```
   *Note: This command executes EF Core migrations, creating the database schema and injecting initial seed data.*

4. **Launch the Application**:
   Set `TMDT` as the startup project and press `F5` to compile and run.

---

## 🤝 Contributing
We follow the standard Git Flow workflow. Please create a feature branch, commit your changes following conventional commit messages, and open a Pull Request for code review.

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
