# Position algorithm configuration

Structures support both the original default-only format and operation-specific
positioning.

## Version 1

Version 1 remains supported and is treated as the default configuration for
every operation.

```json
{
  "v": 1,
  "root": "profile",
  "relation": "relative",
  "groups": [
    { "id": 0, "algo": "classic", "weight": 1 }
  ]
}
```

## Version 2

Version 2 requires `default` and allows overrides under `operations`. A missing
override falls back to `default`.

```json
{
  "v": 2,
  "default": {
    "root": "profile",
    "relation": "relative",
    "groups": [
      { "id": 0, "algo": "classic", "weight": 1 }
    ]
  },
  "operations": {
    "buy_first_place": {
      "root": "owner",
      "relation": "relative",
      "groups": [
        {
          "id": 0,
          "algo": "chess",
          "weight": 1,
          "profiled_places_prioritized": true,
          "depth_spread": 2
        }
      ]
    }
  }
}
```

Supported operation keys are:

- `buy_place`
- `buy_first_place`
- `buy_system_place`
- `create_clone`
- `create_reinvest`

The next-position endpoint accepts the same values through its optional
`operation` query parameter:

```http
GET /api/program/{marketing_addr}/structures/{structure_number}/next-pos?profile_addr={profile_addr}&operation=buy_place
```

When `operation` is omitted, the endpoint uses `default`. An unknown value is
rejected. A valid operation without an override uses `default`.

## Supported algorithms

### `classic`

Places are added from left to right. An explicitly requested position is
honored only by `classic`; `chess` and `radar` always use their calculated
candidate. Candidate selection excludes locked branches and terminal clones.

### `chess` and `radar`

Both algorithms accept these group options:

- `profiled_places_prioritized`: when `true`, search profiled places before
  system places;
- `depth_spread`: the number of open depth levels included in the candidate
  window, starting with the highest open depth.

Locks belong to the resolved root profile. With a profile root, resolution may
fall back through active inviters until it finds the first inviter that has a
place in the structure; that inviter's locks are then used.

### `trimmed_classic`

`trimmed_classic` uses classic positioning and accepts `cut_factor`. It is
intended for the `create_clone` and `create_reinvest` operation overrides.

```json
{
  "v": 2,
  "default": {
    "root": "profile",
    "relation": "relative",
    "groups": [
      { "id": 0, "algo": "classic", "weight": 1 }
    ]
  },
  "operations": {
    "create_clone": {
      "root": "profile",
      "relation": "relative",
      "groups": [
        {
          "id": 0,
          "algo": "trimmed_classic",
          "weight": 1,
          "cut_factor": 2
        }
      ]
    }
  }
}
```

The factor must be at least `2`. The processor counts existing direct clone
children of the selected parent. Every Nth clone, including a reinvest, is
created as kind `2` instead of the ordinary clone kind `1`. Kind `2` is a
terminal clone: it cannot receive children and is excluded from every
next-position candidate query.

### `profile_frontier` and `system_gap`

These algorithms are intended to be configured together through version 2
operation overrides. `profile_frontier` limits the number of profiled places
that do not yet have a profiled child. System children do not remove a place
from this profiled frontier.

While the frontier is below `profiled_frontier_limit`, profiled places expand
in breadth-first order. Parents at the same depth receive profiled children
evenly from left to right. Once the limit is reached, only frontier places can
receive the next profiled child: the old parent leaves the frontier and the new
child enters it, so the limit is not increased. Candidate leaves are compared
by profiled subtree load at every branch from the root; the least-loaded branch
wins, with depth and left-to-right MP order breaking ties. This keeps profiled
descendant counts balanced after the frontier reaches its limit. Profiled
places are never placed beneath system places.

`system_gap` fills the highest open position first and uses MP order from left
to right at the same depth. It cannot consume the final available child slot
of a profiled place that has no profiled child, preserving a route for the
profiled structure to continue. With structure width `1`, this means the end
of a profiled chain is unavailable to system places.

```json
{
  "v": 2,
  "default": {
    "root": "owner",
    "relation": "relative",
    "groups": [
      { "id": 0, "algo": "profile_frontier", "weight": 1,
        "profiled_frontier_limit": 35 }
    ]
  },
  "operations": {
    "buy_system_place": {
      "root": "owner",
      "relation": "relative",
      "groups": [
        { "id": 0, "algo": "system_gap", "weight": 1 }
      ]
    }
  }
}
```

Use the root strategy appropriate for the program. Both algorithms respect
the resolved root subtree, terminal clones, structure width, activity, and
position locks.

For an existing structure, the pgAdmin-compatible
`Database/Scripts/set_structure_profile_frontier_algorithm.sql` script sets
the profile-frontier and system-gap configuration by completely replacing the
structure's existing `pos_algo` value.
