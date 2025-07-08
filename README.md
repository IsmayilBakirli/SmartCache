# SmartCache API

SmartCache is a scalable, high-performance ASP.NET Core 8 Web API designed to efficiently manage **Categories**, **Services**, and **Stories**. It employs **Redis caching** with a **version-based synchronization** strategy to reduce database load and optimize client-server data communication. The backend is powered by **Entity Framework Core** with **PostgreSQL**, all orchestrated via **Docker** for easy deployment and environment consistency.

---

## Table of Contents

1. [Overview](#overview)  
2. [Technologies & Tools](#technologies--tools)  
3. [Architecture & Design](#architecture--design)  
4. [Caching Strategy with Redis](#caching-strategy-with-redis)  
5. [Version-Based Cache Invalidation](#version-based-cache-invalidation)  
6. [API Endpoints Overview](#api-endpoints-overview)  
7. [Error Handling](#error-handling)  
8. [Docker Setup & Deployment](#docker-setup--deployment)  
9. [Running the Application](#running-the-application)  
10. [Folder Structure](#folder-structure)  
11. [Author](#author)

---

## Overview

SmartCache aims to provide:

- **Fast API responses** by leveraging Redis cache to store frequently accessed data.
- **Data consistency** between client and server using a **versioning system** that tracks changes.
- **Scalable architecture** that can easily grow with application demands.
- **Ease of deployment** with containerization using Docker.

---

## Technologies & Tools

- **ASP.NET Core 8** – Modern cross-platform framework for building Web APIs.
- **Entity Framework Core + PostgreSQL** – For robust data persistence and querying.
- **Redis** – In-memory data structure store used for caching to boost read performance.
- **StackExchange.Redis** – .NET client library to communicate with Redis.
- **Docker** – Containerization platform to package API, Redis, and PostgreSQL consistently.
- **Swagger (OpenAPI)** – Auto-generated API documentation and testing UI.
- **Serilog** – Structured logging framework.
- **Middleware** – Centralized exception handling.

---

## Architecture & Design

### Onion Architecture

This project follows the **Onion Architecture** principle to achieve a clean separation of concerns and improve maintainability, testability, and scalability.

The architecture is divided into distinct layers:

- **Core Layer:** Contains the domain entities and business logic.
- **Application Layer:** Implements business rules, services, and interfaces.
- **Infrastructure Layer:** Responsible for data access implementations, caching (Redis), external services, and other infrastructure concerns.
- **Presentation Layer:** Handles HTTP API controllers and user interaction.

By organizing the codebase this way, the project ensures loose coupling between layers and enhances flexibility for future changes.

### Layers & Responsibilities

- **API Layer (Controllers):** Handles HTTP requests, routes to appropriate services, returns structured responses.
- **Service Layer:** Business logic and caching/versioning mechanisms.
- **Repository Layer:** Data access abstraction using Entity Framework Core.
- **Redis Cache:** Stores serialized DTO lists and single items, plus version keys.
- **DTOs:** Data Transfer Objects for decoupling API contracts from entities.
- **Exception Handling:** Middleware catches exceptions, formats JSON error responses.

### Versioning Concept

- Every entity type (`categories`, `services`, `stories`) has a **version integer** stored in Redis.
- When data changes (create/update/delete), this version increments.
- Clients store the version alongside cached data.
- Clients can query the server with their version via `/has-changed/{clientVersion}` to quickly know if they need to refresh data.

---

## Caching Strategy with Redis

### Why Cache?

- Reduces database query load for frequent GET requests.
- Improves latency by serving data from in-memory cache.
- Scales better for read-heavy workloads.

### Cache Keys

| Purpose            | Key Format               | Description                       |
|--------------------|--------------------------|---------------------------------|
| List cache         | `{entity}:all`            | Cached list of all items         |
| Single item cache  | `{entity}:{id}`           | Cached single item detail        |
| Version key        | `{entity}:version`        | Integer version of entity cache  |

### Cache Expiry

- Default TTL for cached data is **10 minutes**.
- Version keys do not expire automatically, only incremented on data change.

### Cache Read Flow

1. **Get All:** Try reading from Redis list cache.
2. If cache miss → Fetch from DB → Cache result → Return data + current version.
3. **Get By Id:** Try single item cache → fallback to list cache to find item → fallback to DB → cache individual item.

### Cache Write Flow

- On **Create**:
  - Insert entity into DB.
  - Add new DTO to cached list if exists; otherwise initialize cache.
  - Cache individual item.
  - Increment version.
- On **Update**:
  - Update entity in DB.
  - Update individual cache.
  - Remove cached list (forcing reload on next get all).
  - Increment version.
- On **Delete**:
  - Delete entity from DB.
  - Remove individual cache and list cache.
  - Increment version.

---

## Version-Based Cache Invalidation

### Problem Addressed

Without versioning, clients do not know if cached data is stale unless they pull full data frequently.

### Solution

- Clients store last known version.
- Query `/has-changed/{clientVersion}` endpoint before fetching full data.
- Server compares client version to current Redis version.
- If no change → returns HTTP 400 with message "No changes detected."
- If changed → client fetches fresh data with new version.

---

## API Endpoints Overview

### CategoriesController (`/api/categories`)

| Method | Endpoint                    | Description                         |
|--------|-----------------------------|-----------------------------------|
| GET    | `/`                        | Get all categories with version   |
| GET    | `/{id}`                    | Get category by ID                 |
| GET    | `/has-changed/{clientVersion}` | Check if category data has changed |
| POST   | `/`                        | Create a new category              |
| PUT    | `/`                        | Update an existing category        |
| DELETE | `/{id}`                    | Delete a category                  |

### ServicesController (`/api/services`)

Identical endpoint structure, managing services.

### StoriesController (`/api/stories`)

Identical endpoint structure, managing stories.

---

## Error Handling

- Global middleware captures exceptions.
- Returns standardized JSON error responses:

```json
{
  "code": 400,
  "message": "No changes detected.",
  "data": null
}
