# TMS API Versioning Policy

## Purpose

The TMS API uses URL-based API versioning to allow the API to evolve
without unexpectedly breaking existing clients. The current versions
are V1 and V2.

## Breaking Changes

A change is considered breaking when an existing client may stop working
correctly after the change.

Examples of breaking changes include:

- Removing an existing response field.
- Renaming an existing request or response field.
- Changing an existing HTTP status code.
- Tightening validation rules so previously valid requests become invalid.
- Changing the default sort order of an existing endpoint.

Breaking changes require a new API version.

## Additive (Non-Breaking) Changes

A change is considered non-breaking when existing clients can continue
working without modification.

Examples include:

- Adding a new optional response field.
- Adding a new endpoint.
- Adding a new optional query parameter.
- Adding functionality without changing the existing contract.

Additive changes may be introduced without creating a new API version,
provided that existing behavior remains compatible.

## Sunset Policy

When a new API version is released, the previous version will remain
available for a minimum of six months.

This gives clients enough time to migrate, particularly training centres
that operate on quarterly maintenance schedules.

For example, when V2 replaces V1, V1 will remain available during the
six-month migration window before it is shut down.

## Communication

When an API version is deprecated, the API will communicate the
deprecation through:

- The `Deprecation` response header.
- The `Sunset` response header.
- The `Link` response header pointing to the successor version.
- A CHANGELOG entry describing the change.
- An email notification to teams that hold an API key.
- A calendar notification for the planned shutdown date.

These communications begin when the new API version is released.

## Skipping Versions

Clients are not required to migrate through every API version.

For example, a client using V1 may migrate directly to V3 if V3 is the
appropriate supported version. Intermediate versions do not need to be
adopted.

## Versioning Strategy

URL-segment versioning is the primary versioning strategy for the TMS API,
for example:

`/api/v1/courses`

and

`/api/v2/courses`

This makes the API version visible in requests and simplifies debugging
and incident response.

Any alternative versioning mechanism must be introduced as an explicit
partner-specific decision and documented before use.