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

After creating or activating a place, the processor walks upward by the configured
structure height. If that height cannot be reached, it uses the last parent reached,
or the affected place when it has no parent. If the required height was not
reached, the response code is `0`; otherwise the code is the number of places
at the created place's level below the resolved source.

For a height-zero structure, the affected place is its own source.

## Activation

`activate_place` targets one existing profiled place. Its payload is exactly the
place number as `uint32`; the task structure and profile identify the rest of
the place key. Activation is allowed only when the command is configured for
the structure, the structure has non-null `activity` JSON, the place has a
profile, and `activated_at` is null. Structure `0` follows the same rules.

Activation always sets `activated_at`. The extensible activity setting
`set_active_on_activation` defaults to `true` inside a non-null activity object;
when false, activation leaves `is_active` unchanged. A successful activation
increments the curator's first-place personal volume in the activated structure
when that place exists, resolves its response source exactly like a purchase,
and records the result through the shared Marketing-task idempotency boundary.

Paid purchases, clones, and reinvest clones start active and activated. Only a
profile's first paid place in any structure greater than `0` activates its
structure-0 invite. Once any such place exists, later paid-place creation never
changes the invite, even if an integration command reset its activation date.
