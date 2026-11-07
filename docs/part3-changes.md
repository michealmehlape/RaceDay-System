# Part 3: Changes to the Part 2 API

| Change | Reason |
|---|---|
| Added `GET /api/events/mine` (Organiser only) | The Organiser dashboard needs every event the Organiser owns, including past ones, with category and enrolment totals. The existing `GET /api/events` only returns upcoming events from all organisers. |