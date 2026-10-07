# UI module

The UI module stores the profiles a wallet intends to display in the frontend.
It replaces the browser-only profile list previously kept in local storage.

The stored relationship is an **intent**, not proof that a wallet owns a
profile. On-chain ownership is checked separately and stored in the `owned`
field as the latest known state.

## Projects

- `UI.Core` contains the profile and wallet-profile intent domain models.
- `UI.Application` contains commands, queries, contract synchronization, and
  domain-event handlers.
- `UI.Dto` contains API response models, modes, and stable error codes.
- `UI.Infrastructure` contains PostgreSQL persistence, queries, repositories,
  address normalization, and dependency registration.
- `UI.Presentation` contains the FastEndpoints API endpoints.

## Data model

```text
profiles
   1
   |
   | profile_addr
   *
wallet_profile_intents        wallet_profile_intent_events
(current frontend intent)     (append-only history)
```

### `profiles`

Caches data read from the Profile NFT contract:

- `address`: canonical Profile NFT address and primary key.
- `login`: normalized lowercase login; unique.
- `content`: profile content stored as `jsonb`.
- `updated_at`: time the cached data last changed.

The cache is refreshed when a profile is added and when a wallet's profiles
are checked. A content or login change raises an in-process
`ProfileContentChangedDomainEvent`.

### `wallet_profile_intents`

Stores the current profile-display intention of a wallet:

- `wallet_addr`: normalized TON wallet address in user-friendly,
  non-bounceable, URL-safe format.
- `profile_addr`: cached Profile NFT address.
- `mode`: `owner` or `preview`.
- `owned`: whether the wallet owned the profile during the latest successful
  contract check.
- `created_at` and `updated_at`: UTC timestamps.

The `(wallet_addr, profile_addr)` combination is unique.

Incoming wallet addresses may be raw or user-friendly, bounceable or
non-bounceable, and URL-safe or standard Base64. Every accepted representation
is converted to the user-friendly, non-bounceable, non-test-only, URL-safe form
before persistence. As a result, different textual representations of the same
TON account resolve to one wallet intent list.

Because the wallet address is a route segment, non-URL-safe Base64 input must
be URL-encoded by the client before it is sent.

`mode` and `owned` deliberately mean different things:

- `mode` records how the wallet intended to use the profile in the UI.
- `owned` records the last verified on-chain ownership state.

Losing ownership sets `owned` to `false`; it does not remove the relationship
or silently change `mode` to `preview`. This preserves the original intent.

### `wallet_profile_intent_events`

Stores append-only relationship history:

- `added`: a new display intent was created.
- `removed`: the wallet removed an existing display intent.
- `ownership_lost`: a successful check observed an `owned: true` to
  `owned: false` transition.
- `ownership_gained`: a successful check observed an `owned: false` to
  `owned: true` transition. This includes both newly acquired and restored
  ownership; it is not recorded as another `added` event.

Each event includes wallet and profile addresses, UTC occurrence time, and a
`jsonb` data object containing relevant mode and ownership values. Events are
inserted by domain-event handlers in the same `SaveChanges` operation as the
current relationship change.

An `added` event is emitted only when the display intent is first created.
Later ownership changes never emit another `added` event.

## TON connection metadata

`ton_connections` stores one latest snapshot per canonical wallet address,
independently of profiles. It is not a connection history or an authenticated session.

- `wallet_addr`: primary key, normalized using the same rules as profile intents.
- `contract_version`: recognized standard code revision (`v1r1` through `v5r1`,
  including `v4r1` and `v4r2`); unrecognized code or absent StateInit is
  `unknown version`.
- `wallet_name`: TON Connect wallet display name, falling back to `device.appName`.
- `app_version`: `device.appVersion`, separate from the contract version.
- `platform`: `device.platform` reported by the wallet, not the browser user agent.
- `created_at`, `updated_at`: UTC timestamps; `updated_at` changes only when metadata changes.
- `last_connected_at`: server UTC time of the latest accepted connection snapshot,
  including restored sessions after a page reload. Also refreshed on metadata-change
  notifications; ordinary React renders do not send snapshots. Existing rows remain
  NULL until the first snapshot after migration 004 (no historical time is guessed).
  This is receipt time, not the original time at which the wallet session was established.

```http
PUT /api/ui/wallets/{wallet_addr}/ton-connection
Content-Type: application/json

{
  "wallet_state_init": "<base64 StateInit BOC from account.walletStateInit>",
  "wallet_name": "Tonkeeper",
  "app_version": "5.0.0",
  "platform": "android"
}
```

Returns `{ "success": true, "errors": [] }`; validation failures return
`success: false` with `err_invalid_wallet_address`, `err_invalid_ton_connection`,
or `err_invalid_wallet_state_init`. Names/version/platform are required, trimmed,
control-character-free strings limited to 128/64/32 characters respectively.
StateInit is optional and limited to 65,536 Base64 characters. If supplied, it
must be valid and hash to the wallet address. The server identifies the code
using the [standard wallet hashes](https://docs.ton.org/contracts/standard/wallets/history).
The StateInit itself is not persisted. This identifies initial code supplied by
TON Connect, not a fresh read of current on-chain code after a contract upgrade.

The repository uses a single atomic PostgreSQL upsert; duplicate connections
cannot create duplicate rows. Identical metadata preserves `updated_at` but refreshes
`last_connected_at`; concurrent requests cannot move that timestamp backwards. The latest
processed changed snapshot replaces all four metadata fields while preserving
`created_at`. Different apps/devices for one address share that same record.
As with profile intents, this anonymous endpoint stores client-reported metadata;
it is not proof of wallet ownership.

The frontend syncs both existing/restored sessions and connection status events,
without waiting for a selected profile. Saves are serialized and temporary
failures are retried up to three attempts; failures do not interrupt wallet use.
A page reload or a later status event can retry a failed snapshot.

Deployment: apply `Database/Scripts/003_create_ton_connections.sql` to the
configured UI database (or Programs fallback), setting its API role variable,
apply `Database/Scripts/004_add_last_connected_at.sql`, then deploy the API and frontend. The API does not apply this migration itself.
New installations need the numbered UI schema scripts in order.

## Contract lookup

Profile resolution uses the Contracts module through MediatR request/response
queries:

1. Resolve the Profile NFT address from the normalized login.
2. Read the Profile NFT data.
3. Cache its login and content.
4. Compare its owner address with the normalized wallet address.

The UI module does not directly call Contracts infrastructure.

## API

Business failures for add, remove, and check are returned in the response body
using `success: false` and stable error codes. This lets the frontend map each
code to localized text without parsing server messages.

### Add a profile intent

```http
POST /api/ui/wallets/{wallet_addr}/profiles
Content-Type: application/json

{
  "login": "alice",
  "mode": "owner"
}
```

Valid modes are `owner` and `preview`.

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": ["owner", "preview"]
}
```

Behavior:

- The login is resolved through the Profile contracts before anything is
  added.
- `owner` succeeds only when the wallet currently owns the profile.
- `preview` succeeds for any wallet when the profile is valid.
- Requesting `owner` without ownership returns
  `err_contract_doesnot_belong_to_the_wallet` and offers `preview` in
  `available_modes`.
- Profile lookup failures return an empty `available_modes` array.
- Adding an existing wallet-profile relationship is idempotent: it does not
  create another `added` event.
- An existing intent's mode is not changed by another add request. To replace
  the intent mode, remove the relationship and add it again.
- Even for an existing relationship, cached content and current ownership are
  refreshed. A detected ownership loss is recorded.

### Remove a profile intent

```http
DELETE /api/ui/wallets/{wallet_addr}/profiles/{login}
```

Example success:

```json
{
  "success": true,
  "errors": [],
  "available_modes": []
}
```

Removal does not call the blockchain. This ensures a wallet can remove its UI
intent even when the contracts provider is unavailable. A successful removal
adds a `removed` history event. Removing a relationship that does not exist
returns `err_profile_relationship_not_found`.

### Check all profiles for a wallet

```http
POST /api/ui/wallets/{wallet_addr}/profiles/check
```

The profiles are checked sequentially. For each relationship, the operation:

1. Reads the current profile contract data by its cached login.
2. Refreshes cached content when it changed.
3. Refreshes the `owned` value.
4. Records `ownership_lost` for a verified `true` to `false` transition.

Example response:

```json
{
  "success": true,
  "errors": [],
  "profiles": [
    {
      "wallet_addr": "EQ...",
      "profile_addr": "EQ...",
      "login": "alice",
      "mode": "owner",
      "owned": true,
      "content": {
        "login": "alice",
        "image_url": "https://example.com/image.png"
      }
    }
  ]
}
```

Checks are partially tolerant. Successfully read profiles are updated even if
another profile fails. In that case `success` is `false`, predefined errors
are returned, and the response still contains the current stored profile list.

### List a wallet's profiles

```http
GET /api/ui/wallets/{wallet_addr}/profiles
```

Returns the current relationships joined with cached profile content. It does
not invoke the blockchain. Use the check endpoint when fresh ownership and
content information is required.

## Error codes

| Code | Meaning |
| --- | --- |
| `err_wallet_not_connected` | Wallet address was not provided. |
| `err_invalid_wallet_address` | Wallet address is not a valid TON address. |
| `err_invalid_login` | Login is empty. |
| `err_invalid_profile_mode` | Add mode is missing or invalid. |
| `err_profile_not_found` | A valid deployed profile could not be found. |
| `err_contract_request_failed` | Contract data could not be read. |
| `err_contract_doesnot_belong_to_the_wallet` | Owner mode was requested by a non-owner wallet. |
| `err_profile_relationship_not_found` | The requested current intent does not exist. |

These constants are defined in `UI.Dto/UiErrorCodes.cs` and should be mirrored
in the frontend error-code map.

## Database setup

For production migration through pgAdmin, use the
[pgAdmin scripts and walkthrough](Database/PgAdmin/README.md).

For a fresh installation with no existing UI tables, run:

```text
src/Modules/UI/Database/Scripts/001_create_ui_profile_intents.sql
```

Before execution, set `v_database_username` inside the script to the database
role used by the API. When migrating existing data, follow the pgAdmin guide
instead; restoring its backup also creates the tables.

The preferred configuration is a dedicated connection:

```dotenv
ConnectionStrings__UI=Host=127.0.0.1;Port=5432;Database=DATABASE_NAME;Username=DATABASE_USER;Password=DATABASE_PASSWORD
```

If `ConnectionStrings__UI` is empty or absent, the module uses
`ConnectionStrings__Programs`. Run the schema script in whichever database is
selected.

## Frontend migration

The frontend can replace its browser profile storage with this sequence:

1. On wallet connection, call the list endpoint.
2. Call check when fresh contract state is needed.
3. Use the add endpoint for owner or preview selection.
4. If owner mode fails and `available_modes` contains `preview`, show the
   localized ownership warning and allow the user to retry in preview mode.
5. Use the remove endpoint instead of deleting only local storage.
6. Treat `mode` as user intent and `owned` as the verified status when choosing
   warnings and UI capabilities.

## Authentication limitation

The endpoints currently follow the rest of the API and are anonymous. A caller
can therefore submit another wallet's address. The data represents a claimed
wallet intention until wallet-signature authentication is added. Do not use it
as cryptographic evidence of wallet behavior or ownership.

## Generated profile avatars

`GET /avatar?login=alice` (also `/api/ui/avatar?login=alice`) returns a 512×512
SVG with a dimensional solid-color CS monogram, a CRYPTO STYLE ribbon,
subtle orbital decoration, and centered login text below it. The accent palette
and orbit angle are derived deterministically from the login hash.
The seed is SHA-256 of the trimmed, lowercase UTF-8 login, interpreted as the
Profile Collection's unsigned big-endian NFT index. The login identifies the
avatar; the accent color alone is not guaranteed unique. No database or
TON request is required, so avatars also work before profile deployment.

The avatar endpoint accepts 4–20 ASCII letters/digits/hyphens, with a letter
or digit at each end, matching profile creation and the documented 20-character
profile login maximum.
Long avatar labels shrink to fit within the frame. Invalid input returns HTTP 400. Successful images use
`image/svg+xml` and one-day public caching. The frontend uses this URL when an
image is omitted during creation/update and displays generated avatars for
cached profiles with missing images or the old shared default. Existing NFT
metadata is not rewritten automatically. Deploy the API before the frontend.

### Indexer SVG compatibility

All visible lettering is embedded vector geometry. The login uses open-licensed
serif outlines and the brand uses sans-serif outlines; indexers need no system
fonts. Letter depth uses offset copies rather than SVG filter primitives, which
can suppress shapes in image proxies. The generator has no runtime font or
native rendering dependency. See `UI.Application/Features/Avatars/Assets/README.md`
for source fonts, licenses, and regeneration instructions.

Existing NFT image URLs remain valid. External indexers may retain an older
rasterized preview after deployment; the API cannot purge their caches. Refresh
through the indexer when available, or update the NFT image URL with a new
version query parameter using the normal wallet-approved profile update.


## TON connection PostgreSQL regression test

From the backend repository root:

```bash
bash tests/Modules/UI.Infrastructure.Tests/Postgres/run.sh --no-restore -m:1 /nodeReuse:false
```

The runner creates a disposable PostgreSQL 17 Docker container on a random
loopback port, applies migrations 003 and 004, and exercises the actual
`TonConnectionRepository` under a restricted application role. It checks legacy
NULL timestamps, initial insertion, reconnection with unchanged metadata,
metadata changes, concurrent inserts, and monotonic `last_connected_at`.
The container is removed on exit; application database configuration is not used.
The test is skipped in ordinary test runs without the runner's explicit port.
