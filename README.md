# FreeDriver - Platform Connecting Drivers and Car Owners

## Overview
**PCDACO** is a platform designed to connect car owners with unused vehicles to licensed drivers who wish to rent and use these vehicles. This system aims to reduce idle car time for owners and provide an affordable income opportunity for drivers, addressing mobility and financial challenges in Vietnam.

---

## Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Technologies Used](#technologies-used)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Setup Instructions](#setup-instructions)
- [Project Structure](#project-structure)
- [API Documentation](#api-documentation)
- [Contributing](#contributing)
- [License](#license)

---

## Architecture
The project adheres to **Clean Architecture** principles, comprising the following layers:
- **Presentation Layer**: Mobile and web-based interfaces for car owners and drivers.
- **Application Layer**: Business logic, managing interactions between users and services.
- **Domain Layer**: Core entities, such as users, vehicles, and bookings.
- **Infrastructure Layer**: Handles data storage, payment integration, and communication with third-party services.
- **Persistence Layer**: Manages the connection to the database and data access implementations.

---

## Features

### Car Owners
- Register, log in, and manage account details.
- List and manage cars, including model, location, availability, and pricing.
- Approve or reject booking requests.
- Track car usage, including distance, fuel level, and damages.
- Manage earnings and view transaction history.
- Leave feedback for drivers.

### Drivers
- Register, log in, and manage account details.
- Search for cars based on location, price, model, and availability.
- Book cars and manage rental details.
- Track ongoing rides and pay for rentals via integrated gateways.
- Leave feedback for car owners.

### Admin
- Manage user accounts and validate registrations.
- Review and approve car listings.
- Monitor financial transactions and platform usage.
- Provide customer support and ensure adherence to platform guidelines.

---

## Technologies Used
- ASP.NET Core Web API
- Mobile and Web Frameworks (React Native, Angular/React)
- Payment Gateways Integration
- GPS and Location Tracking Services
- Entity Framework Core
- SQL Server or PostgreSQL for database management

---

## Getting Started

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download)
- Database Server (PostgreSQL)

### Setup Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/PCDACO/PCDACO-BE.git
   ```
2. Navigate to the project directory:
   ```bash
   cd PCDACO-BE
   ```
3. Install dependencies:
   ```bash
   dotnet restore
   ```
4. Configure database and environment variables in `appsettings.json` or `.env`.
5. Apply database migrations:
   ```bash
   dotnet ef database update
   ```
6. Run the backend.

---

## Project Structure
```
project-root
├── src
│   ├── FreeDriver.Api           # Backend API
│   ├── FreeDriver.Application   # Business Logic
│   ├── FreeDriver.Domain        # Core Entities
│   ├── FreeDriver.Infrastructure # Data Storage and Services
│   └── FreeDriver.Persistence   # Database Connection and Access
├── tests                        # Unit and Integration Tests
└── README.md
```

---

## API Documentation
Detailed API documentation is available at `/swagger` when the backend is running.

---

## Contributing
We welcome contributions! To contribute:
1. Fork the repository.
2. Create a new feature branch.
3. Commit your changes.
4. Open a pull request.

---

## License
This project is licensed under the MIT License.

