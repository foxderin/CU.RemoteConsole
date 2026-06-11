#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_URL="${CU_REMOTE_CONSOLE_URL:-http://127.0.0.1:8848}"
EXPECTED_VERSION="$(
  sed -n 's/.*PluginVersion = "\([^"]*\)".*/\1/p' "$ROOT/src/CU.RemoteConsole/RemoteConsolePlugin.cs" | tail -1
)"

source "$ROOT/scripts/read-token.sh"

auth_config() {
  printf 'header = "Authorization: Bearer %s"\n' "$CU_REMOTE_CONSOLE_TOKEN"
}

json_get() {
  local path="$1"
  curl -fsS --config <(auth_config) "$BASE_URL$path"
}

json_post() {
  local path="$1"
  local body="$2"
  curl -fsS \
    --config <(auth_config) \
    -H "Content-Type: application/json" \
    -d "$body" \
    "$BASE_URL$path"
}

echo "Checking health..."
health="$(curl -fsS "$BASE_URL/health")"
printf '%s' "$health" | EXPECTED_VERSION="$EXPECTED_VERSION" node -e 'let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const h = JSON.parse(raw);
  const expected = process.env.EXPECTED_VERSION;
  if (h.pluginVersion !== expected) {
    throw new Error(`expected pluginVersion=${expected}, got ${h.pluginVersion}`);
  }
  if (!h.httpListening) throw new Error("httpListening=false");
  if (!h.patchApplied) throw new Error("patchApplied=false");
  console.log(`health ok: version=${h.pluginVersion}, queueDepth=${h.queueDepth}, bridge=${h.bridgeLastStatus}`);
});'

echo "Checking unauthorized recent-history rejection..."
unauth_code="$(curl -sS -o /tmp/cu-remoteconsole-noauth.json -w '%{http_code}' "$BASE_URL/api/commands")"
if [[ "$unauth_code" != "401" ]]; then
  echo "expected 401 for unauthenticated /api/commands, got $unauth_code" >&2
  exit 1
fi
echo "unauthenticated recent history rejected"

echo "Checking command catalog..."
catalog="$(json_get /api/commands/catalog)"
printf '%s' "$catalog" | node -e 'let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const data = JSON.parse(raw);
  const items = Array.isArray(data.items) ? data.items : [];
  const help = items.find(item => item.name === "help");
  const kill = items.find(item => item.name === "kill");
  if (!help || help.classification !== "Safe" || help.allowed !== true) {
    throw new Error("catalog does not mark help as allowed Safe");
  }
  if (!kill || kill.classification !== "Dangerous" || kill.allowed !== false) {
    throw new Error("catalog does not mark kill as denied Dangerous");
  }
  console.log(`catalog ok: items=${items.length}`);
});'

echo "Checking read-only status..."
status_json="$(json_get /api/status)"
printf '%s' "$status_json" | EXPECTED_VERSION="$EXPECTED_VERSION" node -e 'let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const data = JSON.parse(raw);
  const expected = process.env.EXPECTED_VERSION;
  if (data.pluginVersion !== expected) throw new Error(`expected pluginVersion=${expected}, got ${data.pluginVersion}`);
  if (!data.network || data.network.bindAddress !== "127.0.0.1") throw new Error("status network bindAddress mismatch");
  if (!data.security || data.security.authRequired !== true) throw new Error("status authRequired mismatch");
  if (!data.policy || data.policy.allowedCount < 1) throw new Error("status policy summary missing");
  console.log(`status ok: version=${data.pluginVersion}, bind=${data.network.bindAddress}:${data.network.port}, allowed=${data.policy.allowedCount}`);
});'

echo "Submitting safe command: help"
submit="$(json_post /api/commands '{"command":"help"}')"
queue_id="$(printf '%s' "$submit" | node -e 'let raw=""; process.stdin.on("data", c => raw += c); process.stdin.on("end", () => process.stdout.write(JSON.parse(raw).queueId || ""));')"
if [[ ${#queue_id} -ne 32 ]]; then
  echo "invalid queue id length from submit response" >&2
  exit 1
fi
echo "submit accepted: queueIdLength=${#queue_id}"

receipt=""
for _ in $(seq 1 20); do
  receipt="$(json_get "/api/commands/$queue_id")"
  state="$(printf '%s' "$receipt" | node -e 'let raw=""; process.stdin.on("data", c => raw += c); process.stdin.on("end", () => process.stdout.write(JSON.parse(raw).state || ""));')"
  if [[ "$state" != "Queued" ]]; then
    break
  fi
  sleep 0.2
done

printf '%s' "$receipt" | node -e 'let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const r = JSON.parse(raw);
  if (r.state !== "Executed") throw new Error(`expected Executed, got ${r.state}`);
  if (r.commandName !== "help") throw new Error(`expected help, got ${r.commandName}`);
  if (r.classification !== "Safe") throw new Error(`expected Safe, got ${r.classification}`);
  if (r.bridgeStatus !== "executed") throw new Error(`expected bridgeStatus=executed, got ${r.bridgeStatus}`);
  const outputLines = Array.isArray(r.output) ? r.output.length : 0;
  if (outputLines < 1) throw new Error("expected at least one output line");
  if (r.outputLineCount !== outputLines) throw new Error(`expected outputLineCount=${outputLines}, got ${r.outputLineCount}`);
  if (typeof r.outputTruncated !== "boolean") throw new Error("expected boolean outputTruncated");
  console.log(`receipt ok: state=${r.state}, bridge=${r.bridgeStatus}, latencyMs=${r.queueLatencyMs}, outputLines=${outputLines}`);
});'

echo "Checking recent history..."
recent="$(json_get /api/commands)"
printf '%s' "$recent" | node -e 'let raw = "";
process.stdin.on("data", chunk => raw += chunk);
process.stdin.on("end", () => {
  const data = JSON.parse(raw);
  const items = Array.isArray(data.items) ? data.items : [];
  if (!items.length) throw new Error("recent history is empty");
  console.log(`recent ok: items=${items.length}`);
});'

echo "Smoke test passed."
