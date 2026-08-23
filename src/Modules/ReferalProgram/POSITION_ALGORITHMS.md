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
