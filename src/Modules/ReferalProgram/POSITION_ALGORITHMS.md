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
