# RaceDay

RaceDay is a race-event management system that lets **Organisers** create and
manage running events, and lets **Participants** browse events, enrol in a
category, and view their results. This repository contains Part 1: the
system planning and database design for RaceDay, produced before any
application code was written.

## Roles

| Role | Can do |
|---|---|
| **Organiser** | Create/update/delete their own events and categories, view enrolments for their categories, and capture results. |
| **Participant** | Browse public events and categories, enrol in a category, view and cancel their own enrolments, and view results/leaderboards. |

## Repository structure

```
/docs
  erd.png              -> Entity Relationship Diagram
  endpoint_plan.md      -> Full API endpoint plan (Section B)
  raceday_schema.sql    -> Database creation + seed script (Section C)
/.github/workflows
  validate.yml           -> checks that /docs contains the required files
README.md
```

## Database design

The database has six entities: **Users, Events, EventOrganisers, Categories,
Enrolments, Results**. See `docs/erd.png` for the full diagram, including
primary keys, foreign keys, and relationship cardinality. `EventOrganisers`
is a junction table that models the many-to-many relationship between
Organisers and Events (an event can have more than one organiser assisting
it, and an organiser can help run more than one event), in addition to each
event's single primary `OrganiserID`.



## CI/CD

A GitHub Actions workflow (`.github/workflows/validate.yml`) runs on every
push/PR to `main` and confirms that the `/docs` folder exists and contains
the ERD, endpoint plan, and SQL script, and that `README.md` exists at the
repository root.



<img width="2879" height="993" alt="image" src="https://github.com/user-attachments/assets/5c3716c3-5140-4180-860a-7cc1f0e51c38" />


## Video walkthrough

Video walking through the  ERD Diagram, endpoint plan choices, and a live run of the SQL script in SSMS:

`> https://youtu.be/iHSIG5ppvxs `
