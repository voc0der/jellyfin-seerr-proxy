# jellyfin-seerr-proxy

`jellyfin-seerr-proxy` is a minimal Jellyfin plugin that lets authenticated Jellyfin clients submit Seerr/Jellyseerr media requests as the currently logged-in Jellyfin user.

The plugin keeps Seerr credentials on the Jellyfin server. A client such as Wholphin calls the Jellyfin plugin endpoint with its normal Jellyfin auth token; the plugin resolves that Jellyfin user to the linked Seerr user and submits the Seerr request with `X-API-User` set server-side.

## What It Does

- Exposes authenticated Jellyfin endpoints under `/Plugins/SeerrProxy`.
- Resolves the current Jellyfin user from Jellyfin authentication claims.
- Looks up the linked Seerr user with `GET /api/v1/user/jellyfin/{jellyfinUserId}`.
- Creates requests with `POST /api/v1/request`.
- Sends `X-Api-Key` and `X-API-User` only from server-side plugin configuration and resolved identity.
- Returns clear JSON errors suitable for TV clients.

## What It Does Not Do

- It does not create Jellyfin libraries.
- It does not create placeholder media.
- It does not sync discovery content.
- It does not hook favorites or watch state.
- It does not change the Jellyfin library experience.

## Required Seerr Setup

Jellyfin users must already be imported or linked in Seerr/Jellyseerr. The plugin does not create or link Seerr users.

Create or copy a Seerr API key and store it in the Jellyfin plugin configuration page. Clients never need this key.

## Endpoints

All endpoints require Jellyfin authentication.

### `GET /Plugins/SeerrProxy/Status`

Returns plugin state and, when configured and enabled, whether the current Jellyfin user maps to a Seerr user. Secrets are never returned.

### `GET /Plugins/SeerrProxy/User`

Returns a small linked-user projection:

```json
{
  "jellyfinUserId": "00000000000000000000000000000000",
  "seerrUserId": 7,
  "displayName": "Bob",
  "linked": true
}
```

### `POST /Plugins/SeerrProxy/Request`

Example movie request:

```json
{
  "mediaType": "movie",
  "tmdbId": 9481,
  "is4k": false
}
```

Example TV request:

```json
{
  "mediaType": "tv",
  "tmdbId": 1399,
  "seasons": [1, 2],
  "is4k": false,
  "serverId": 1,
  "profileId": 1,
  "rootFolder": "/tv",
  "languageProfileId": 1,
  "tags": []
}
```

The plugin accepts either `mediaId` or `tmdbId` and sends Seerr's `mediaId`. It passes only safe Seerr request fields: `mediaType`, `mediaId`, `tvdbId`, `seasons`, `is4k`, `serverId`, `profileId`, `rootFolder`, `languageProfileId`, and `tags`.

Client-provided identity fields are ignored. Do not send `userId` or `X-API-User`; the plugin derives the requester from Jellyfin authentication only.

### `POST /Plugins/SeerrProxy/Test`

Dashboard-only elevated endpoint used by the configuration page to test Seerr reachability and the configured API key.

## TV Season Defaults

When a TV request omits `seasons`, the plugin uses the configured default:

- First season only
- All seasons
- Require client-specified seasons

When a client provides explicit seasons, those seasons are used.

## Installation

### Plugin Catalog

1. Open **Dashboard -> Plugins -> Repositories**
2. Add `https://raw.githubusercontent.com/voc0der/jellyfin-seerr-proxy/main/manifest.json`
3. Install **Seerr Proxy** from **Catalog**
4. Restart Jellyfin

### Manual Install

1. Download the latest ZIP from the releases page
2. Extract it into your Jellyfin plugins directory
3. Restart Jellyfin

## Building

Install the .NET 9 SDK, then run:

```bash
dotnet build --configuration Release
```

The plugin DLL is written to `bin/Release/net9.0/Jellyfin.Plugin.SeerrProxy.dll`.

## Release Metadata

`manifest.json` is updated by the release workflow when a new version is published.
