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
increments the profile's personal volume and its current direct curator's
referral volume in the activated structure. The curator does not need a place
in that structure. It resolves its response source exactly like a purchase and
records the result through the shared Marketing-task idempotency boundary.

Activation changes only the selected place. It emits its volume operation;
there is no propagation to other places of the profile, structure, group, or
program. The nullable text `structures."group"` is an organizational label:
trimmed, case-sensitive, and empty values become null. It does not control
activity. Retired `activity.activation_sync` values are ignored.

Paid purchases, clones, and reinvest clones start active and activated. Only a
profile's first paid place in any structure greater than `0` activates its
structure-0 invite. Once any such place exists, later paid-place creation never
changes the invite, even if an integration command reset its activation date.

### Activity configuration rollout: stages 1–4

Legacy activity objects remain supported unchanged, including the default
`set_active_on_activation = true` and ignored retired `activation_sync` values.
Missing activity still disables explicit activation. No data migration or setup
script change is required; CryptoCash retains its existing configuration.

New objects require `type: "invite"` for structure 0 or `type: "marketing"` for
other structures. `preserve_status_on_activation` defaults to false and is the
inverse of legacy `set_active_on_activation`. Mixing the two formats is rejected.
Unknown fields, duplicate keys, incorrect types, and null nested blocks are
invalid in the new format. Omitted boolean options default to false.

Stage 2 enables three structure-0 `when_inactive` options:

- `allow_inviting_without_places`: an inactive inviter can invite if its profile
  has no places in structures greater than 0 in this program.
- `allow_inviting_with_places`: an inactive inviter can invite if such places
  exist. Presence counts purchased places, clones, and reinvests regardless of
  activity or activation date. Places in other programs do not count.
- `allow_as_fallback_root`: profile-root resolution may use an inactive profiled
  inviter when looking for a first place in the target structure. Ancestors
  without a target place are skipped; system invites are skipped and cycles
  terminate the search. The target place itself need not be active, as before.

Missing activity, legacy activity, and omitted/false options preserve the old
rules. Active inviters still invite normally. New invite children remain inactive
with no activation date. Existing-invite and missing-profile checks still apply.
Own first places are returned without reading invite settings. Position selection
uses the resolved root owner's locks. The `owner` strategy used by CryptoCash
remains independent of invite activity and these settings.

Example allowing invitations and fallback through inactive invites:

```json
{
  "type": "invite",
  "when_inactive": {
    "allow_inviting_without_places": true,
    "allow_inviting_with_places": true,
    "allow_as_fallback_root": true
  }
}
```

Stage 3 enables marketing placement settings:

```json
{
  "type": "marketing",
  "when_inactive": {
    "allow_own_children": false,
    "check_manual_placement": false
  },
  "spillover": {
    "allow_inactive_place": false,
    "require_active_invite": false
  }
}
```

Own children are places belonging to the candidate parent's profile or a profile
personally invited by it in structure 0 of the same program. Use the placed
profile, not the payer; the same rule applies to purchases, clones and reinvests.
Other placements, including system children, are spillover for this eligibility
check. A system parent has no profile, so it has no own children and is exempt
from the active-invite requirement; its own activity still matters.

Automatic placement requires an active candidate unless its matching own/spillover
permission permits inactivity. Spillover additionally requires the candidate
owner's active structure-0 first place when `require_active_invite` is enabled.
A missing invite does not satisfy this requirement. Own children are exempt from
this extra invite check. These checks do not mutate dates, flags or volumes.

The filter is applied before pagination, depth-window selection and sorting in
classic, trimmed_classic, empty_parent, chess, radar, profile_frontier and
system_gap. A rejected candidate does not hide its descendants. Width, terminal
clones, locks and algorithm-specific constraints continue to apply.

Manual classic placement retains its activity exception unless
`check_manual_placement` is true. When enabled, it uses the same rules as automatic
placement. Commands recheck the selected parent, and tree purchase actions use a
batched active-invite lookup. Tree rendering does not add a query per node.

Legacy activity JSON remains irrelevant to placement. With no new permissions,
the existing candidate eligibility rules are preserved and no child-invite lookup
is added. Frontier level statistics are aggregated once per level; its previous
selection behavior is covered by differential PostgreSQL tests.
No existing program data or setup script is changed automatically.

Stage 4 enables the following `when_inactive` options for both activity types:

```json
{
  "allow_as_bonus_recipient": false,
  "allow_as_clone_recipient": false,
  "keep_on_compression": false
}
```

Recipient resolution reads settings from the structure being traversed, once per
resolution rather than once per ancestor. Bonus queries use the bonus permission;
clones and reinvests use the clone permission. Referral bonuses resolve the
initial relative place under its own structure's bonus rules, then the inviter
under structure-0 bonus rules. System places remain ineligible. Relative levels
count eligible profiled places, including permitted inactive ones. Root fallback
and the original source/reason place are preserved. Date, status and volumes are
not changed by resolution; creating a paid clone still has its normal volume and
first-paid-place invite-activation effects.

Combined move-or-structure-bonus tasks first resolve the clone recipient. If that
profile has no place in the target structure, the clone branch is selected.
Otherwise, or if no clone recipient exists, the bonus branch independently
resolves under bonus rules and can select another profile. A missing bonus
recipient rejects that branch. With omitted/false rules both searches retain the
old active-profile selection.

Compression defaults to removing inactive and system places and requiring an
active profiled root. `keep_on_compression=true` retains inactive profiled places
and permits them, including an inactive root, to receive children while rebuilding.
It does not activate them or change dates/volumes. System places are still removed;
terminal clones cannot become parents. Width, ordering, locks, rank/volume
priority and matrix-filling recalculation remain in effect. This administrative
rebuild uses its own classic/empty-parent rules, not the ordinary placement
permissions introduced in stage 3.

All declared activity rules now have consumers; the temporary activation error
`activity_rules_not_supported_yet` is retired. Validation occurs when settings are
parsed by their consumers, not as a database constraint or configuration write API.
Legacy activation-only JSON does not affect recipient/compression eligibility.

Equivalent activation examples:

```json
{ "set_active_on_activation": true }
```

```json
{ "type": "marketing", "preserve_status_on_activation": false }
```

CryptoCash regression coverage reads the actual setup JSON for structures 1–4
and exercises activation and successive period resets with both formats.
Reset without a new activation switches the place off; reset after activation
keeps it active and clears its date. Reset adds no activation-volume event.
These tests do not execute PostgreSQL or contact TON.

## Expired first-place task (disabled)

Command `program.structure.deactivate-expired-first-places` and its period/target
format are reserved for a future implementation. Its service is currently a
stub: it returns an explicit disabled error and does not read or mutate places,
flags, volumes, or processed-command records. A scheduler invocation fails
instead of being acknowledged as completed.

There is no separate place-deactivation operation or immediate-deactivation
setting. `ResetActivity` keeps its existing behavior: calculate `is_active`
from the old activation date, then clear that date. It has no volume effect.
See [Scheduled Tasks](../ScheduledTasks/README.md#expired-first-place-task-disabled)
for the reserved command format.

## Profile volume

Volume belongs to `(marketing address, structure number, profile address)`, not
to a place. A missing row means zero. A profiled first-place purchase, regular
purchase, activation, clone, or reinvest currently adds one personal-volume
unit to the operating profile and one referral-volume unit to its direct
curator from structure `0`. Terminal clones are included. Structure `0` follows
the same operation rules. Group volume is stored but is not calculated yet.
