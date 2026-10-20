#!/usr/bin/env bash
# Hook Stop — l'agent ne peut pas déclarer « terminé » si du code C# a changé et que la
# suite de tests est rouge. exit 2 = l'arrêt est refusé et stderr est renvoyé à l'agent,
# qui reprend la main pour corriger.
set -uo pipefail

input="$(cat)"

# Anti-boucle : si l'agent a déjà été relancé par ce hook, on le laisse conclure
# (il expliquera lui-même pourquoi il n'y arrive pas).
# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n ("true\r" ≠ "true" casserait l'anti-boucle).
[ "$(jq -r '.stop_hook_active // false' <<<"$input" 2>/dev/null | tr -d '\r')" = "true" ] && exit 0

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0

# Rien à vérifier si aucun fichier C# ou projet n'a changé depuis le dernier commit.
[ -z "$(git status --porcelain -- '*.cs' '*.csproj' '*.razor' 2>/dev/null)" ] && exit 0

sln="$(ls ./*.slnx ./*.sln 2>/dev/null | head -1)"
[ -n "$sln" ] || exit 0

if ! out="$(dotnet test "$sln" --nologo --verbosity quiet 2>&1)"; then
  {
    echo "Conclusion refusée par le hook tests-must-pass : du code a changé et des tests échouent."
    printf '%s\n' "$out" | grep -E "error |\[FAIL\]|Failed |Échec |Assert|Expected|Actual" | head -15
    echo ""
    echo "Corrige le CODE — jamais le test pour le faire passer —, relance dotnet test, puis conclus."
  } >&2
  exit 2
fi
exit 0
