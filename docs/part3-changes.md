# Part 3: Changes to the Part 2 API

### New endpoints

| Method | Path | Who | Purpose |
|---|---|---|---|
| GET | `/api/events/mine` | Organiser | Lists the Organiser's own events (past and upcoming) with category and enrolment counts. Used by the dashboard. |
| POST | `/api/users/profile/picture` | Any logged-in user | Uploads a profile picture to Azure Blob Storage and saves its URL. JPG, PNG or WEBP, max 2 MB. |
| POST | `/api/events/{id}/banner` | Organiser (owner only) | Uploads an event banner to Azure Blob Storage and saves its URL. JPG, PNG or WEBP, max 5 MB. |

### Azure Blob Storage

- Only the API talks to Azure. The MVC app sends the file to the API and displays the URL the API returns.
- `IBlobStorageService` and `BlobStorageService` (in `RaceDay.Api/Services`) do the upload. The connection string is read only when an upload happens, so the API and the tests start without it.
- The connection string is kept in User Secrets (`AzureBlob:ConnectionString`, `AzureBlob:ContainerName`), never in the repository.
- The container allows anonymous read access for blobs, so browsers can show images from their URL. Uploading still needs the secret key.

### Other changes

- In Development the API skips the HTTPS redirect, so the web container can reach it over plain http through `host.docker.internal`.

## Design decisions and deviations

- **Distance lives on categories** (as in Part 2). The Create event form asks for a main distance, maximum participants and entry fee, and creates the event's first category from them with a second API call.
- **Two sessions.** The web app has its own session cookie (`.RaceDay.Web.Session`). It stores the API's session cookie and sends it with every API call. The names differ because browsers share cookies between ports on the same host.
