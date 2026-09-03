# RaceDay API Endpoint Plan

Roles: **Organiser**, **Participant**. "None" = public, "Any" = any logged-in user.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant. | None | `{ fullName, email, password, role }` | 201 Created – user record (no password) <br> 400 Bad Request – validation error <br> 409 Conflict – email already registered |
| POST | /api/auth/login | Authenticates a user and returns a JWT. | None | `{ email, password }` | 200 OK – `{ token, user }` <br> 401 Unauthorized – invalid credentials |
| GET | /api/users/profile | Returns the logged-in user's own profile. | Any | None | 200 OK – user profile <br> 401 Unauthorized |
| PUT | /api/users/profile | Updates the logged-in user's own profile details. | Any | `{ fullName, email }` | 200 OK – updated profile <br> 400 Bad Request <br> 401 Unauthorized |
| POST | /api/users/profile/picture | Uploads a profile picture to Azure Blob Storage and saves the returned URL to `Users.ProfilePictureUrl`. | Any | Multipart form file (`image`) | 200 OK – `{ profilePictureUrl }` <br> 400 Bad Request – invalid file type/size <br> 401 Unauthorized |
| GET | /api/events | Lists all upcoming events. | None | None | 200 OK – array of events |
| GET | /api/events/{id} | Returns a single event's details. | None | None | 200 OK – event <br> 404 Not Found |
| POST | /api/events | Creates a new event owned by the logged-in Organiser. | Organiser | `{ eventName, eventDate, location, description }` | 201 Created – event <br> 400 Bad Request <br> 403 Forbidden |
| PUT | /api/events/{id} | Updates an event owned by the logged-in Organiser. | Organiser (owner) | `{ eventName, eventDate, location, description }` | 200 OK – updated event <br> 403 Forbidden <br> 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in Organiser. | Organiser (owner) | None | 200 OK <br> 403 Forbidden <br> 404 Not Found |
| POST | /api/events/{id}/banner | Uploads a banner image to Azure Blob Storage and saves the returned URL to `Events.BannerImageUrl`. | Organiser (owner) | Multipart form file (`image`) | 200 OK – `{ bannerImageUrl }` <br> 400 Bad Request – invalid file type/size <br> 403 Forbidden <br> 404 Not Found |
| GET | /api/events/{id}/categories | Lists all categories for a given event. | None | None | 200 OK – array of categories <br> 404 Not Found |
| POST | /api/events/{id}/categories | Adds a new category to an event. | Organiser (owner) | `{ categoryName, distanceKM, maxParticipants, entryFee }` | 201 Created – category <br> 400 Bad Request <br> 403 Forbidden <br> 404 Not Found |
| PUT | /api/categories/{id} | Updates a category. | Organiser (owner) | `{ categoryName, distanceKM, maxParticipants, entryFee }` | 200 OK – updated category <br> 403 Forbidden <br> 404 Not Found |
| DELETE | /api/categories/{id} | Deletes a category. | Organiser (owner) | None | 200 OK <br> 403 Forbidden <br> 404 Not Found |
| POST | /api/categories/{id}/enrol | Enrols the logged-in Participant into a category (bib number auto-generated). | Participant | None | 201 Created – enrolment <br> 404 Not Found <br> 409 Conflict – already enrolled or category full |
| GET | /api/users/enrolments | Lists the logged-in Participant's own enrolments. | Participant | None | 200 OK – array of enrolments |
| GET | /api/categories/{id}/enrolments | Lists all participants enrolled in a category. | Organiser (owner) | None | 200 OK – array of enrolments <br> 403 Forbidden <br> 404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant (owner) | None | 200 OK <br> 403 Forbidden <br> 404 Not Found |
| POST | /api/enrolments/{id}/results | Captures a result for an enrolment. | Organiser (owner) | `{ finishTime, position, status }` | 201 Created – result <br> 400 Bad Request <br> 403 Forbidden <br> 404 Not Found |
| GET | /api/categories/{id}/results | Returns the results/leaderboard for a category. | None | None | 200 OK – array of results, ordered by position <br> 404 Not Found |
| GET | /api/results/{id} | Returns a single result. | None | None | 200 OK – result <br> 404 Not Found |
