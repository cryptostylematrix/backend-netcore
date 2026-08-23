# Referral program processing invariants

This document records behavior enforced by the backend rather than by a
Marketing smart contract. Contract command availability does not override
these rules.

## Purchase prerequisites

When a structure has `prev_required = true`, the profile must already have a
place in the immediately preceding structure. This check runs before command
selection. `buy_first_place` does not bypass it.

`buy_first_place` is selected only when the profile has no place in any
structure whose contract configuration exposes that command. Otherwise the
processor uses `buy_place` when it is available.

Selected positions apply only to `classic`. `chess` and `radar` ignore a
requested position and calculate one. For a profiled classic purchase, an
explicit position must also be inside the profile's resolved subtree and
outside its locks. System-place purchases do not apply the profile-subtree
check, but still validate the requested classic position and its locks.

## Source-place response

After creating a place, the processor walks upward by the configured structure
height. If that height cannot be reached, it uses the last parent reached, or
the created place when it has no parent. If the required height was not
reached, the response code is `0`; otherwise the code is the number of places
at the created place's level below the resolved source.

For a height-zero structure, the created place is its own source.

## Activation

Marketing contracts may expose `activate_place`, and the contract suites test
their configured activation reward branches. Backend activation processing is
intentionally deferred. The task processor currently logs this task as
unsupported and does not mutate a place or send a successful response.

Do not interpret contract-level activation tests as evidence that backend
activation processing is implemented.
