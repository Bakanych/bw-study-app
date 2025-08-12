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

## E2E & Manual Test Strategy

- Use real environment with real production data.
- Target user experiense instead of architecture goals.
- Use explarotory testing to find out non-obvious behaviour and avoid repetition of the same synthetic tests.
- **Mobile/responsive UX (essential):**
    - it's all about mobile experience nowadays. Any web app should be tested from mobile layouts.
    - iPhone/Android device emulation: form fields usable with virtual keyboard; no blocked actions by copy/paste popups; buttons tappable; lists scroll correctly.
- **Cross-browser smoke** Chrome, Edge, Firefox; verify no layout breakage and that the static UI works.
- Carefully test notifications (email, messengers). Can be dangerous zone when testing in real env.
  

- For E2E test automation: aim zero flakiness to earn trust. Do not accept agresive retry patterns to elminate flakiness, fix test setup instead.


## Extra Concurrency & Robustness Tests (integration focus)

- **Race on “join same subject”:** Fire `N` parallel join requests for different groups but same subject and user (e.g., `Task.WhenAll`).  
- **Idempotent leave:** Two concurrent leaves for the same (group, user) should result in zero membership and 200 OK both times.  
- **Transaction boundaries:** Repository operations that modify membership run within transactions; tests assert atomicity (no partial writes).


## Known limitations / Backlog
 
- **Edit/delete groups** not implemented (task scope).  
- **Server-side sort param**: UI implements newest/oldest; API can be extended to support server-side sorting.
- **UI automation**: Playwright can be considered as optimal choice for UI automation for smoke + cross-browser matrix.  
- **Feature flags & rollouts**: Next step—wire a simple env-based flag to hide/show the feature per environment.  
- **Notifications (email/messenger)**: Out of scope; if added, use MailHog/test doubles in non-prod to avoid real deliveries.
