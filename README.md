## Study Groups feature — automated tests and usage

This solution implements the Study Groups feature and an automated test suite covering the acceptance criteria. It includes:

- ASP.NET Core Web API (`StudyApp`) with simple demo static UI in `wwwroot/`
- Unit tests with NUnit (`StudyApp.UnitTests`)
- Integration tests with NUnit + Testcontainers for SQL Server (`StudyApp.IntegrationTests`)

### Acceptance criteria covered

- Users can create only one Study Group per Subject
- Group name length between 5–30 characters
- Valid subjects only: Math, Chemistry, Physics
- Creation date is recorded
- Users can join/leave groups; one group per subject constraint enforced
- Users can list all groups, filter by subject
- Sorting (newest/oldest) is provided in the UI by `createDate`

### Automated test cases implemented

Unit tests in `StudyApp.UnitTests/StudyGroupServiceTests.cs` validate domain/service rules:

- Create group succeeds with valid data and optional empty member list
- Name validation errors for too short/long/blank names
- Reject non-existent user IDs on creation
- Reject creating a group when any provided user is already in a group for the same subject
- Get-by-id returns group when exists and null otherwise
- List all groups; filter by subject
- Join group succeeds; returns false when user already in a same-subject group; throws when group missing
- Leave group is idempotent and removes correct membership

Integration tests spin up the API against real SQL Server via Testcontainers and cover end-to-end API behavior:

- Creating empty and populated groups; invalid subject and invalid name lengths
- Enforcing uniqueness per user per subject on create
- Listing groups with and without subject filter; invalid subject handling
- Getting group by id (found/not found)
- Joining groups (multiple users join, conflict on same-subject membership, joining different subject succeeds)
- Leaving groups (removal, idempotency, and correct member preservation)

All tests above have minimal execution time and are all intended for regression.

### How to run

Prerequisites:

- .NET 9 SDK
- Docker Desktop (for integration tests or running with SQL Server)

Build everything:

```bash
dotnet build
```

Run unit tests only (fast, no Docker required):

```bash
dotnet test StudyApp.UnitTests/StudyApp.UnitTests.csproj
```

Run integration tests (requires Docker running):

```bash
dotnet test StudyApp.IntegrationTests/StudyApp.IntegrationTests.csproj
```

Run all tests:

```bash
dotnet test
```

### Run the app

Local (in-memory DB, default for Development):

```bash
dotnet run --project StudyApp
```

Open `http://localhost:5223`

Docker Compose (SQL Server with data volume):

```bash
docker compose up -d --build
```

Open `http://localhost:8080`

### SQL query

Return all StudyGroups that have at least one user with name starting with "M", ordered by creation date:

```sql
SELECT DISTINCT sg.*
FROM StudyGroups sg
JOIN StudyGroupMembers sgm ON sgm.StudyGroupId = sg.StudyGroupId
JOIN Users u ON u.UserId = sgm.UserId
WHERE u.Name LIKE 'M%'
ORDER BY sg.CreateDate ASC;
```
