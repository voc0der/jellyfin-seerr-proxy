#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

guid_re='[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}'

lower() {
  tr '[:upper:]' '[:lower:]'
}

manifest_guid="$(jq -r '.[0].guid // empty' manifest.json | lower)"

if ! [[ "${manifest_guid}" =~ ^${guid_re}$ ]]; then
  echo "manifest.json does not contain a valid plugin GUID." >&2
  exit 1
fi

plugin_guid="$(grep -Eoh "${guid_re}" Plugin.cs | lower | sort -u)"
config_guid="$(grep -Eoh "${guid_re}" Configuration/configPage.html | lower | sort -u)"

if [ "$(printf '%s\n' "${plugin_guid}" | sed '/^$/d' | wc -l)" -ne 1 ]; then
  echo "Plugin.cs must contain exactly one unique plugin GUID." >&2
  printf '%s\n' "${plugin_guid}" >&2
  exit 1
fi

if [ "$(printf '%s\n' "${config_guid}" | sed '/^$/d' | wc -l)" -ne 1 ]; then
  echo "Configuration/configPage.html must contain exactly one unique plugin GUID." >&2
  printf '%s\n' "${config_guid}" >&2
  exit 1
fi

if [ "${manifest_guid}" != "${plugin_guid}" ] || [ "${manifest_guid}" != "${config_guid}" ]; then
  echo "Plugin GUID mismatch:" >&2
  echo "  manifest.json: ${manifest_guid}" >&2
  echo "  Plugin.cs: ${plugin_guid}" >&2
  echo "  configPage.html: ${config_guid}" >&2
  exit 1
fi

known_taken_guids=(
  "eb5d7894-8eef-4b36-aa6f-5d124e828ce1" # Jellyfin plugin template
  "f5a3c7e1-9b2d-4f6a-8e0c-1d3b5a7c9e2f" # IMDb Ratings
  "fdcf27a6-bd64-42e3-9f57-41880552bf83" # Transcode Nag
  "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d" # K3ntas Ratings
)

for taken_guid in "${known_taken_guids[@]}"; do
  if [ "${manifest_guid}" = "${taken_guid}" ]; then
    echo "Plugin GUID ${manifest_guid} is known to belong to another plugin." >&2
    exit 1
  fi
done

if [ "${CHECK_REMOTE_GUIDS:-0}" = "1" ]; then
  remote_manifests=(
    "https://repo.jellyfin.org/files/plugin/manifest.json"
    "https://raw.githubusercontent.com/voc0der/jellyfin-imdb-rating-updater/main/manifest.json"
    "https://raw.githubusercontent.com/voc0der/jellyfin-transcode-nag/main/manifest.json"
    "https://raw.githubusercontent.com/K3ntas/jellyfin-plugin-ratings/main/manifest.json"
  )

  for manifest_url in "${remote_manifests[@]}"; do
    if curl -fsSL "${manifest_url}" \
      | jq -e --arg guid "${manifest_guid}" 'any(.[]; (.guid | ascii_downcase) == $guid)' >/dev/null; then
      echo "Plugin GUID ${manifest_guid} already appears in ${manifest_url}." >&2
      exit 1
    fi
  done
fi

echo "Plugin GUID ${manifest_guid} is internally consistent and not in the known-taken list."
