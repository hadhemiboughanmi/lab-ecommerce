#!/usr/bin/env bash
# Hook PreToolUse (Bash) — bloque les commandes irréversibles.
set -uo pipefail

# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n.
cmd="$(jq -r '.tool_input.command // empty' 2>/dev/null | tr -d '\r')" || exit 0
[ -z "$cmd" ] && exit 0

DATA='DROP (TABLE|DATABASE|SCHEMA)|dotnet ef database drop|docker (volume rm|system prune)|docker compose down .*(-v|--volumes)'
INFRA='kubectl delete (ns|namespace|pvc)|k3d cluster delete|helm uninstall'
GIT='git push .*(-f|--force)|git reset --hard|git clean -[a-z]*f'

if printf '%s' "$cmd" | grep -qiE "$DATA|$INFRA|$GIT"; then
  {
    echo "BLOQUÉ par le hook guard-destructive : $cmd"
    echo "Commande irréversible. Présente l'impact et l'alternative réversible ;"
    echo "l'humain la lancera lui-même s'il la valide."
  } >&2
  exit 2
fi
exit 0
