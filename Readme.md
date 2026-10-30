# MongoDbTask API

A .NET 10 Web API that imports users from the public [DummyJSON](https://dummyjson.com/users) API into MongoDB and exposes a **cursor-paginated search endpoint** built on native MongoDB queries.

The whole stack (API + MongoDB) runs with a single Docker command, so anyone who clones the repository can test it on any machine without installing .NET or MongoDB.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Quick Start (Docker)](#quick-start-docker)
- [Using the API](#using-the-api)
- [Search Examples](#search-examples)
- [Cursor Pagination in Action](#cursor-pagination-in-action)
- [Why Cursor Pagination?](#why-cursor-pagination)
- [Configuration](#configuration)
- [Running Without Docker](#running-without-docker)
- [Useful Docker Commands](#useful-docker-commands)
- [Troubleshooting](#troubleshooting)

---

## Features

- **Import** users from DummyJSON into MongoDB in pages, using bulk upserts (safe to run multiple times, no duplicates).
- **Search** users with combinable filters: free text, gender, role, city, country, and age range.
- **Cursor-based pagination** (keyset pagination) that stays fast on large collections.
- **Swagger UI** for exploring and testing the endpoints.
- **One-command setup** with Docker Compose (API + MongoDB, with healthcheck and persistent volume).

## Tech Stack

| Component | Technology |
|---|---|
| API | ASP.NET Core (.NET 10) |
| Database | MongoDB 8 |
| MongoDB driver | MongoDB.Driver for .NET |
| API docs | Swagger / Swashbuckle |
| Containers | Docker & Docker Compose |

## Project Structure

```
MongoDbTask/
├── src/
│   └── MongoDbTask.API/
│       ├── Controllers/                 # HTTP endpoints (UsersController)
│       ├── DBContext/                   # MongoDbContext (connection and collections)
│       ├── Models/                      # Request/response models (UserSearchRequest, CursorPage, ...)
│       ├── Options/                     # Strongly-typed configuration (MongoDbOptions, APIOption)
│       ├── Services/                    # UserImportService, UserSearchService, DummyJsonClient
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── MongoDbTask.API.csproj
│       └── Program.cs
├── .dockerignore
├── .gitattributes
├── .gitignore
├── docker-compose.yml                   # API + MongoDB services
├── Dockerfile                           # Multi-stage build for the API
├── MongoDbTask.slnx
└── README.md
```

## Quick Start (Docker)

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Docker Engine + the Compose plugin)
- [Git](https://git-scm.com/)

You do **not** need .NET or MongoDB installed.

### Steps

```bash
# 1. Clone the repository
https://github.com/abdelrahmanalimohamed/MongoDbTask.git
cd MongoDbTask

# 2. Build and start everything
docker compose up --build
```

The first run downloads the images and builds the API, so it takes a little longer. When the API is up, open:

**http://localhost:5000/swagger**

### What happens behind the scenes

```mermaid
flowchart LR
    Client["Browser / Postman / curl"] -->|"http://localhost:5000"| API["api container<br/>.NET 10 (port 8080)"]
    API -->|"mongodb://mongodb:27017"| Mongo[("mongodb container<br/>MongoDB 8")]
    API -->|"HTTPS (import only)"| Dummy["dummyjson.com/users"]
    Mongo --- Vol[("mongo-data volume")]
```

1. Compose starts the **MongoDB** container and waits until its healthcheck passes.
2. Only then does it start the **API** container, which connects to MongoDB using the service name `mongodb` as the host.
3. Data is stored in the `mongo-data` Docker volume, so it survives restarts.

## Using the API

> **Important:** the database starts empty. Run the import first.

### 1. Import users

Imports all users from DummyJSON into MongoDB. Running it again updates existing users instead of duplicating them.

```bash
curl -X POST http://localhost:5000/api/users/import
```

Example response:

```json
{
  "processed": 208,
  "pagesFetched": 7,
  "total": 208
}
```

### 2. Search users

```
GET /api/users/search
```

| Parameter | Type | Description |
|---|---|---|
| `search` | string | Case-insensitive text matched against first name, last name, username and email |
| `gender` | string | Exact match, case-insensitive (e.g. `male`, `female`) |
| `role` | string | Exact match, case-insensitive (e.g. `admin`, `moderator`, `user`) |
| `city` | string | Exact match, case-insensitive |
| `country` | string | Exact match, case-insensitive |
| `minAge` | int | Minimum age (0-150) |
| `maxAge` | int | Maximum age (0-150) |
| `limit` | int | Page size, 1-100 (default `20`) |
| `cursor` | int | The `nextCursor` value from the previous page. Omit for the first page |

All filters are optional and can be combined. Invalid values (for example `limit=500`) return `400 Bad Request`.

**Response shape**

```json
{
  "items": [ { "id": 12, "firstName": "...", "lastName": "...", "username": "...", "email": "...",
               "phone": "...", "age": 31, "gender": "male", "role": "user",
               "city": "...", "country": "..." } ],
  "nextCursor": 57
}
```

`nextCursor` is the value to send as `cursor` to get the next page. When it is `null`, there are no more results.

## Search Examples

> The sample data below is illustrative. Your exact results depend on the data returned by DummyJSON.

| Goal | Request |
|---|---|
| First 20 users (no filters) | `GET /api/users/search` |
| Smaller page | `GET /api/users/search?limit=5` |
| Text search (name, username or email) | `GET /api/users/search?search=john` |
| By gender | `GET /api/users/search?gender=female&limit=10` |
| By role | `GET /api/users/search?role=admin` |
| By age range | `GET /api/users/search?minAge=25&maxAge=35` |
| By location | `GET /api/users/search?country=United%20States&city=Phoenix` |
| Combined filters | `GET /api/users/search?search=an&gender=male&minAge=30&maxAge=50&limit=10` |
| Next page | `GET /api/users/search?gender=female&limit=10&cursor=57` |

With `curl`, let it encode spaces for you using `-G` and `--data-urlencode`:

```bash
curl -G "http://localhost:5000/api/users/search" \
  --data-urlencode "country=United States" \
  --data-urlencode "minAge=25" \
  --data-urlencode "limit=5"
```

### How a request becomes a MongoDB query

`GET /api/users/search?search=john&minAge=25&limit=20&cursor=30` runs:

```js
db.users.find({
  externalId: { $gt: 30 },
  $or: [
    { firstName: /john/i }, { lastName: /john/i },
    { username: /john/i },  { email: /john/i }
  ],
  age: { $gte: 25 }
}).sort({ externalId: 1 }).limit(21)   // limit + 1, to detect if another page exists
```

## Cursor Pagination in Action

Here is a full walk-through, paging through all female users **3 at a time**. Notice that each response tells the client exactly where to continue.

**Page 1:** no cursor

```
GET /api/users/search?gender=female&limit=3
```
```json
{
  "items": [ { "id": 1, "...": "..." }, { "id": 3, "...": "..." }, { "id": 5, "...": "..." } ],
  "nextCursor": 5
}
```

**Page 2:** pass `nextCursor` from page 1

```
GET /api/users/search?gender=female&limit=3&cursor=5
```
```json
{
  "items": [ { "id": 6, "...": "..." }, { "id": 8, "...": "..." }, { "id": 11, "...": "..." } ],
  "nextCursor": 11
}
```

**Last page:** fewer results than the limit, so there is no next cursor

```
GET /api/users/search?gender=female&limit=3&cursor=204
```
```json
{
  "items": [ { "id": 205, "...": "..." }, { "id": 208, "...": "..." } ],
  "nextCursor": null
}
```

Ids are not consecutive because the filter skips users that do not match; the cursor always continues after the **last id returned**.

### Fetch everything with a loop

**PowerShell**

```powershell
$cursor = $null
do {
    $url = "http://localhost:5000/api/users/search?gender=female&limit=20"
    if ($cursor) { $url += "&cursor=$cursor" }

    $page = Invoke-RestMethod $url
    $page.items | ForEach-Object { "$($_.id)  $($_.firstName) $($_.lastName)" }

    $cursor = $page.nextCursor
} while ($cursor)
```

**Bash** (requires `jq`)

```bash
cursor=""
while :; do
  page=$(curl -s "http://localhost:5000/api/users/search?gender=female&limit=20${cursor:+&cursor=$cursor}")
  echo "$page" | jq -r '.items[] | "\(.id)  \(.firstName) \(.lastName)"'
  cursor=$(echo "$page" | jq -r '.nextCursor // empty')
  [ -z "$cursor" ] && break
done
```

## Why Cursor Pagination?

The common alternative is **offset pagination** (`skip` + `limit`, or `?page=50`). It works for small data but has real problems as data grows:

| | Offset (`skip`/`limit`) | Cursor (this API) |
|---|---|---|
| **Speed on deep pages** | Gets slower: MongoDB must walk past and discard every skipped document (`skip(100000)` reads 100,000 entries first) | Constant: jumps straight to `externalId > cursor` using the index and reads only `limit + 1` documents |
| **Stable while data changes** | No: an insert or delete while the client is paging shifts everything, causing **duplicates or missing items** | Yes: "everything after id 57" does not change when other rows are added or removed |
| **Extra count query** | Usually needed to know if there is a next page | Not needed: the API fetches `limit + 1` documents instead |
| **Jump to page N** | Yes | No, forward-only |
| **Best for** | Small datasets, numbered page buttons | Infinite scroll, feeds, exports, large collections |

### Example of the stability problem

A client is on page 1 of 3 items per page and reads users `1, 2, 3`. A new user is then inserted at the front of the sort order.

- **Offset:** page 2 is `skip(3).limit(3)`. Everything has shifted by one, so user `3` appears **again** at the top of page 2 (a duplicate).
- **Cursor:** page 2 is `externalId > 3`. User `3` can never appear again, and nothing is skipped.

### See the speed difference yourself

Open a shell in the MongoDB container:

```bash
docker compose exec mongodb mongosh -u admin -p password123 --authenticationDatabase admin MongoLearningDb
```

Compare the two approaches and look at `totalDocsExamined` in the output:

```js
// Offset: must walk past the first 150 documents before returning 20
db.users.find().sort({ externalId: 1 }).skip(150).limit(20)
  .explain("executionStats").executionStats.totalDocsExamined      // ~170

// Cursor: seeks directly to the position using the index
db.users.find({ externalId: { $gt: 150 } }).sort({ externalId: 1 }).limit(21)
  .explain("executionStats").executionStats.totalDocsExamined      // ~21
```

With 208 users the gap is small, but the offset cost grows linearly with the page number while the cursor cost stays flat, so the difference becomes dramatic on collections with hundreds of thousands of documents.

## Configuration

Default values live in `src/MongoDbTask.API/appsettings.json`:

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://admin:password123@localhost:27017/?authSource=admin",
    "DatabaseName": "MongoLearningDb",
    "UsersCollectionName": "users"
  },
  "APIOption": {
    "URL": "https://dummyjson.com/users"
  }
}
```

Docker Compose overrides these with environment variables (`__` maps to `:` in .NET configuration):

| Environment variable | Purpose | Value in Docker |
|---|---|---|
| `MongoDb__ConnectionString` | MongoDB connection string | `mongodb://admin:password123@mongodb:27017/?authSource=admin` |
| `MongoDb__DatabaseName` | Database name | `MongoLearningDb` |
| `MongoDb__UsersCollectionName` | Collection name | `users` |
| `APIOption__URL` | Source API for the import | `https://dummyjson.com/users` |
| `ASPNETCORE_ENVIRONMENT` | Enables Swagger when `Development` | `Development` |

> Inside Docker the MongoDB host is **`mongodb`** (the service name), not `localhost`.

> **Security note:** the credentials in this repo are for local development only. Do not reuse them in a real deployment; use a `.env` file or a secrets manager instead.

## Running Without Docker

Useful if you want to debug the API from Visual Studio or another IDE.

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and a MongoDB instance.

```bash
# 1. Start only MongoDB from Docker Compose (exposed on localhost:27017)
docker compose up -d mongodb

# 2. Run the API locally (uses the localhost connection string from appsettings.json)
dotnet run --project src/MongoDbTask.API
```

The console prints the local URL the API is listening on. Open `/swagger` on that URL.

You can inspect the data with [MongoDB Compass](https://www.mongodb.com/products/tools/compass) using:

```
mongodb://admin:password123@localhost:27017/?authSource=admin
```
