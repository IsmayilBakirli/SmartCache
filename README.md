# SmartCache API

SmartCache is a scalable, high-performance ASP.NET Core 8 Web API designed to efficiently manage **Categories**, **Services**, and **Stories**. It employs **Redis caching** with a **version-based synchronization** strategy to reduce database load and optimize client-server data communication. The backend is powered by **Entity Framework Core** with **Microsoft SQL Server**, all orchestrated via **Docker** for easy deployment and environment consistency.

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
- **Entity Framework Core + Microsoft SQL Server** – For robust data persistence and querying.  
- **Redis** – In-memory data structure store used for caching to boost read performance.  
- **StackExchange.Redis** – .NET client library to communicate with Redis.  
- **Docker** – Containerization platform to package API, Redis, and Microsoft SQL Server consistently.  
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

- Reduces database load by serving frequent GET requests from Redis cache.  
- Improves response times and scalability.

### Cache Keys & Expiry

| Purpose           | Key Format          | Description                   |
|-------------------|---------------------|-------------------------------|
| List cache        | `{entity}:all`      | Cached list of all items.     |
| Single item cache | `{entity}:{id}`     | Cached detail of a single item. |
| Version key       | `{entity}:version`  | Current integer version of the entity. |

- Cached data expires after 10 minutes (TTL).  
- Version keys do not expire; updated only on data changes.

### Cache Read Flow

- **GetAll**: Read from `{entity}:all` cache, fallback to DB and cache if miss.  
- **GetById**: Read from `{entity}:{id}` cache, fallback to DB and cache if miss.

### Cache Write Flow

- **Create**: Add to DB, cache individual item, append to cached list if present, increment version.  
- **Update**: Update DB and individual cache, invalidate cached list, increment version.  
- **Delete**: Remove from DB and both caches, increment version.

---

## Version-Based Cache Invalidation

Clients track last synced versions and query the server to detect changes before fetching data:

- Clients POST their known versions to `/api/sync/check-versions`.  
- Server returns entities with updated versions.  
- Clients refresh only changed data, reducing unnecessary API calls.

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
## Docker Setup & Deployment

This project uses Docker Compose to run the ASP.NET Core API, Microsoft SQL Server database, and Redis cache in separate containers.

### Prerequisites

- Docker
- Docker Compose

### Running the Containers

1. Build and start containers:

```bash
docker-compose up --build

## Running the Application

After setting up Docker containers, you can run and access the SmartCache API as follows:

### Steps

1. Ensure Docker and Docker Compose are installed and running on your machine.

2. Start the application stack with:

```bash
docker-compose up --build
## Running the Application

1. Ensure Docker and Docker Compose are installed on your system.  
2. Run `docker-compose up -d` in the root project directory to start the API, Redis, and MSSQL Server containers.  
3. Access the API Swagger UI at `http://localhost:5252/swagger` (or your configured port) to test endpoints.  
4. For simulating client behavior, run the provided console application which interacts with the Sync API to minimize unnecessary data fetching.  
5. Modify connection strings or environment variables in `.env` or `docker-compose.override.yml` as needed, especially the MSSQL connection string.  
6. The default MSSQL connection string is:  
   `Server=sqlserver,1433;Database=SmartCacheDb;User=sa;Password=K1a2m3A4l;Encrypt=False;TrustServerCertificate=True;`