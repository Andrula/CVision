# CVision.Api.Tests

Comprehensive unit test suite for the CVision API, covering core business logic and controller validation.

## Test Coverage

### Services

#### 1. **JobService Tests** (`Services/JobServiceTests.cs`)
Tests for job management and skill distribution analytics:

- ✅ **GetSkillDistributionAsync** - Critical business logic
  - Groups and counts skills correctly across multiple candidates
  - Handles duplicate skills
  - Filters whitespace and empty values
  - Sorts results by count (descending)
  - Handles null/empty JSONB data gracefully

- ✅ **GetAllJobsAsync** - Returns all jobs with applicant counts
- ✅ **CreateJobAsync** - Validates title and description, handles errors
- ✅ **DeleteJobAsync** - Deletes existing jobs, handles missing jobs
- ✅ **GetJobByIdAsync** - Retrieves jobs by ID

**Total Tests: 17**

---

#### 2. **CandidateService Tests** (`Services/CandidateServiceTests.cs`)
Tests for CV upload orchestration and candidate management:

- ✅ **UploadCandidateAsync** - Complex multi-step workflow
  - Creates candidate record
  - Calls parser service
  - Saves file to storage
  - Creates profile with parsed data
  - Updates candidate name
  - Handles parser failures gracefully
  - Validates file and job ID

- ✅ **GetCandidatesForJobAsync** - Filters by job ID
- ✅ **GetCandidatesWithMatchScoreAsync** - Returns match scores
- ✅ **GetProfileAsync** - Retrieves candidate profiles
- ✅ **SaveProfileAsync** - Serializes lists to JSONB
- ✅ **GetCandidateCvStreamAsync** - Streams CV files

**Total Tests: 20**

---

#### 3. **FileStorageService Tests** (`Services/FileStorageServiceTests.cs`)
Tests for file I/O operations:

- ✅ **SaveFileAsync**
  - Generates unique filenames with GUID prefix
  - Saves correct content to disk
  - Creates directories if needed
  - Validates file is not null/empty

- ✅ **GetFileStreamAsync**
  - Returns stream for existing files
  - Returns null for missing files
  - Logs warnings appropriately

- ✅ **DeleteFileAsync**
  - Deletes existing files
  - Handles missing files gracefully

- ✅ **FileExists** - Checks file existence
- ✅ **Full Lifecycle Test** - Save → Get → Delete integration

**Total Tests: 20**

---

#### 4. **PythonCVParserService Tests** (`Services/PythonCVParserServiceTests.cs`)
Tests for Python parser HTTP integration:

- ✅ **ParseCandidateFromFileAsync**
  - Deserializes valid JSON responses
  - Handles all DTO fields correctly
  - Throws on invalid JSON
  - Throws on null responses
  - Handles HTTP errors (400, 500, timeout)
  - Validates match score range (0-100)
  - Sends correct URL and multipart form data
  - Case-insensitive JSON deserialization
  - Handles empty skill arrays
  - Network error handling

**Total Tests: 14**

---

### Controllers

#### 5. **CandidatesController Tests** (`Controllers/CandidatesControllerTests.cs`)
Tests for candidate API endpoints:

- ✅ **GetCandidates** - Returns 200 OK with candidates
- ✅ **Upload**
  - Returns 200 OK on success
  - Returns 400 Bad Request for invalid file
  - Returns 404 Not Found for invalid job ID
  - Returns 500 on service errors

- ✅ **GetProfile** - Returns 200 OK or 404 Not Found
- ✅ **SaveProfile** - Returns 200 OK or 500 on error
- ✅ **GetCandidateCV** - Returns file stream or 404

**Total Tests: 10**

---

#### 6. **JobsController Tests** (`Controllers/JobsControllerTests.cs`)
Tests for job API endpoints:

- ✅ **GetJobs** - Returns 200 OK with all jobs
- ✅ **GetJob** - Returns 200 OK or 404 Not Found
- ✅ **GetCandidatesForJob** - Returns candidates with match scores
- ✅ **CreateJob**
  - Returns 201 Created with location header
  - Returns 400 Bad Request for validation errors
  - Returns 500 on service errors

- ✅ **DeleteJob** - Returns 204 No Content or 404
- ✅ **GetSkillDistribution** - Returns skill analytics

**Total Tests: 12**

---

## Test Helpers

### `TestHelpers.cs` (`Helpers/TestHelpers.cs`)
Utility class providing:

- ✅ In-memory database context creation
- ✅ Test data seeding
- ✅ Mock entity factories (Job, Candidate, CandidateProfile)
- ✅ Mock DTO factories
- ✅ Mock IFormFile creation
- ✅ JSONB serialization/deserialization helpers

---

## Running the Tests

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 / VS Code / Rider (or any IDE with .NET support)

### Command Line

```bash
# Restore packages
dotnet restore CVision.Api.Tests/CVision.Api.Tests.csproj

# Build the test project
dotnet build CVision.Api.Tests/CVision.Api.Tests.csproj

# Run all tests
dotnet test CVision.Api.Tests/CVision.Api.Tests.csproj

# Run with detailed output
dotnet test CVision.Api.Tests/CVision.Api.Tests.csproj --verbosity detailed

# Run with code coverage
dotnet test CVision.Api.Tests/CVision.Api.Tests.csproj --collect:"XPlat Code Coverage"
```

### Visual Studio
1. Open the solution in Visual Studio
2. Go to **Test** → **Test Explorer**
3. Click **Run All** to execute all tests

### VS Code
1. Install the **C# Dev Kit** extension
2. Open the Test Explorer
3. Run tests from the Test Explorer panel

---

## Test Statistics

| Category | Test Count | Status |
|----------|-----------|--------|
| JobService | 17 | ✅ Complete |
| CandidateService | 20 | ✅ Complete |
| FileStorageService | 20 | ✅ Complete |
| PythonCVParserService | 14 | ✅ Complete |
| CandidatesController | 10 | ✅ Complete |
| JobsController | 12 | ✅ Complete |
| **Total** | **93** | ✅ Complete |

---

## Dependencies

- **xUnit** (2.5.3) - Testing framework
- **Moq** (4.20.70) - Mocking library
- **FluentAssertions** (6.12.0) - Assertion library
- **Microsoft.EntityFrameworkCore.InMemory** (9.0.4) - In-memory database for testing
- **Microsoft.NET.Test.Sdk** (17.8.0) - Test SDK
- **coverlet.collector** (6.0.0) - Code coverage

---

## What's Tested

### ✅ Core Business Logic
- Skill distribution aggregation and counting
- Match score calculations validation
- JSONB serialization/deserialization
- File storage with unique filename generation

### ✅ Data Validation
- Input validation (null checks, empty strings)
- Match score range validation (0-100)
- Required field validation
- File validation

### ✅ Error Handling
- ArgumentException for invalid inputs
- InvalidOperationException for business rule violations
- HTTP error responses (400, 404, 500)
- Network timeouts and failures

### ✅ Integration Points
- Python parser HTTP communication
- File storage I/O operations
- Database operations with EF Core
- Multipart form data handling

### ✅ Logging
- Information logging for successful operations
- Warning logging for missing resources
- Error logging for exceptions

---

## Future Enhancements

### Potential Additions:
1. **Integration Tests** - Test full API flow with TestServer
2. **Performance Tests** - Benchmark critical operations
3. **Data Validation Tests** - Add FluentValidation tests for DTOs
4. **Authentication Tests** - When auth is implemented
5. **Database Migration Tests** - Verify EF migrations

---

## Notes

- All tests use **in-memory databases** to avoid external dependencies
- **HTTP calls are mocked** using `HttpMessageHandler` to avoid external API calls
- **File I/O tests** use temporary directories that are cleaned up after each test
- Tests follow the **AAA pattern** (Arrange, Act, Assert)
- Each test class implements `IDisposable` for proper resource cleanup

---

## Troubleshooting

### Tests fail with "dotnet command not found"
- Install .NET 8.0 SDK from https://dotnet.microsoft.com/download

### Tests fail with package restore errors
- Run `dotnet restore` in the test project directory
- Clear NuGet cache: `dotnet nuget locals all --clear`

### InMemory database tests fail
- Ensure `Microsoft.EntityFrameworkCore.InMemory` package is installed
- Check that each test uses a unique database name (handled by `TestHelpers.CreateInMemoryDbContext()`)

---

**Created for GitHub Issue #7: Add unit tests for core business logic**
