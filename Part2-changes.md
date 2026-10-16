# Part 2: Changes from the Part 1 plan

## Database
| Change | Reason |
|---|---|
| Added `EventType` (Run, Walk or Cycle, with a CHECK constraint) to `Events` | The Part 2 brief requires each event to have a type. Distance stays on `Categories`, because one event can offer several distances (10km, 21km). |
| The API uses a new database, `RaceDayApiDB`, created from code-first migrations | EF Core builds the schema from the model classes. Otherwise it matches the Part 1 script. |

## Authentication
| Change | Reason |
|---|---|
| Server-side session instead of a JWT | The Part 2 brief asks for session-based authentication storing the user ID and role. |
| Added `POST /api/auth/logout` | A session needs a way to end. |
| Added `organiserCode` to `POST /api/auth/register` | Stops anyone from registering as an Organiser by choosing the role. |
| Passwords must be at least 8 characters; emails are stored lowercase | Basic validation, and prevents duplicate accounts that differ only by case. |

## Endpoints added
| Endpoint | Reason |
|---|---|
| `GET /api/users/results` | The brief says Participants must be able to view their own results, and the Part 1 plan had no endpoint for this. |
| `GET /api/events?eventType=` (optional filter) | Prepares the Part 3 home page filtering. |

## Endpoints deferred to Part 3
| Endpoint | Reason |
|---|---|
| `POST /api/users/profile/picture` | Needs Azure Blob Storage, which the brief places in Part 3. |
| `POST /api/events/{id}/banner` | Same reason. The `ProfilePictureUrl` and `BannerImageUrl` columns already exist. |

## Extra status codes and rules
| Change | Reason |
|---|---|
| `409 Conflict` when deleting an event that still has categories | Protects data (foreign keys use restrict, not cascade). |
| `409 Conflict` when deleting a category with enrolments, or lowering its limit below current enrolments | Same reason. |
| `400 Bad Request` when enrolling in an event that has already taken place | Business rule. |
| `409 Conflict` when cancelling an enrolment that already has a result | Keeps results consistent. |
| Result statuses are `Finished`, `DNF` and `DNS`; a Finished result needs a time and a position; positions are unique per category | Makes the data meaningful. |
| `totalFinishers` is included in result responses | Lets Part 3 show "47th out of 312 finishers". |