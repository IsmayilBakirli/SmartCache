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

Layers:

- **Core Layer:** Domain entities and business logic.  
- **Application Layer:** Business rules, services, and interfaces.  
- **Infrastructure Layer:** Data access, caching (Redis), external services.  
- **Presentation Layer:** HTTP API controllers and user interaction.

---

### Layers & Responsibilities

- **API Layer (Controllers):** Handle HTTP requests and responses.  
- **Service Layer:** Business logic plus caching/versioning mechanisms.  
- **Repository Layer:** Data access via Entity Framework Core.  
- **Redis Cache:** Stores serialized DTOs and version keys.  
- **DTOs:** Decouple API contracts from entities.  
- **Exception Handling:** Middleware catches exceptions and formats JSON error responses.

---

### Versioning Concept

- Each entity (`categories`, `services`, `stories`) has a **version integer** in Redis.  
- On data changes (create/update/delete), the version increments.  
- Clients store the version with cached data.  
- Clients check data freshness via a centralized **SyncController** endpoint.

---

## Caching Strategy with Redis

### Why Cache?

- Reduces DB load for frequent GETs.  
- Improves response latency.  
- Scales well for read-heavy usage.

### Cache Keys

| Purpose           | Key Format        | Description                     |
|-------------------|-------------------|---------------------------------|
| List cache        | `{entity}:all`    | Cached list of all items         |
| Single item cache | `{entity}:{id}`   | Cached single item detail        |
| Version key       | `{entity}:version`| Current version integer          |

### Cache Expiry

- Default TTL: **10 minutes** for cached data.  
- Version keys do not expire; only increment on data changes.

### Cache Read Flow

1. Try reading from Redis list cache.  
2. On miss, fetch from DB, cache, then return with current version.  
3. For single item, try single cache → fallback to list cache → fallback to DB → cache individual item.

### Cache Write Flow

- **Create:** Insert DB → add to cached list or init → cache item → increment version.  
- **Update:** Update DB → update cache item → remove cached list → increment version.  
- **Delete:** Delete DB → remove cache item & list → increment version.

---

## Version-Based Cache Invalidation

### Problem

Clients cannot know if their cached data is stale without frequent full data fetches.

### Solution

- Clients keep last known versions.  
- Call **SyncController**'s POST `/api/sync/check-versions` endpoint with client versions.  
- Server compares and returns which entities have changed.  
- Clients decide to refresh only if versions differ.

---

## API Endpoints Overview

### CategoriesController (`/api/categories`)

| Method | Endpoint  | Description              |
|--------|-----------|--------------------------|
| GET    | `/`       | Get all categories with version.  |
| GET    | `/{id}`   | Get category by ID.      |
| POST   | `/`       | Create a new category.   |
| PUT    | `/`       | Update an existing category.  |
| DELETE | `/{id}`   | Delete a category.       |

### ServicesController (`/api/services`)

Same endpoint structure as CategoriesController, for services.

### StoriesController (`/api/stories`)

Same endpoint structure as CategoriesController, for stories.

### SyncController (`/api/sync`)

| Method | Endpoint                | Description                                      |
|--------|-------------------------|------------------------------------------------|
| POST   | `/check-versions`       | Check if entity versions have changed compared to client versions. |

---

## Error Handling

- Global middleware captures exceptions and returns JSON error responses.

Example:

```json
{
  "code": 400,
  "message": "No changes detected.",
  "data": null
}
