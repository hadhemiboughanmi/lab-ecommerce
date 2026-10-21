#!/usr/bin/env bash
# Hook PostToolUse (Bash) — après un `docker build -t <image>`, scanne l'image avec Trivy.
#
# Informe, ne casse rien : sans vulnérabilité CRITICAL corrigeable, exit 0 et rien n'est dit.
# Sinon, exit 2 : le résumé est renvoyé à l'agent, qui doit qualifier et proposer une
# remédiation. Le BLOCAGE, lui, est dans la CI, là où une procédure d'équipe existe pour le lever.
# Un scan qui n'a pas pu avoir lieu est SIGNALÉ (exit 2) : un contrôle muet fait croire à une
# protection qui n'existe pas.
set -uo pipefail

# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n.
cmd="$(jq -r '.tool_input.command // empty' 2>/dev/null | tr -d '\r')" || exit 0
printf '%s' "$cmd" | grep -q 'docker build' || exit 0

image="$(printf '%s' "$cmd" | sed -nE 's/.*(-t|--tag)[ =]+([^ ]+).*/\2/p' | head -1)"
[ -n "$image" ] || exit 0
docker image inspect "$image" >/dev/null 2>&1 || exit 0     # build échoué : rien à scanner

# Trivy installé par winget peut ne pas être dans le PATH d'une session déjà ouverte.
trivy="$(command -v trivy 2>/dev/null)"
if [ -z "$trivy" ]; then
  trivy="$(ls "${LOCALAPPDATA:-/nonexistent}"/Microsoft/WinGet/Packages/AquaSecurity.Trivy*/trivy.exe 2>/dev/null | head -1)"
fi
if [ -z "$trivy" ]; then
  echo "Image $image NON scannée : trivy introuvable. Installe-le (winget install AquaSecurity.Trivy) puis relance le build." >&2
  exit 2
fi

if ! report="$("$trivy" image --quiet --severity CRITICAL --ignore-unfixed --format table "$image" 2>&1)"; then
  {
    echo "Image $image NON scannée : Trivy a échoué."
    printf '%s\n' "$report" | tail -5
  } >&2
  exit 2
fi

if printf '%s' "$report" | grep -q 'CRITICAL'; then
  {
    echo "Scan Trivy de $image : vulnérabilité(s) CRITICAL corrigeable(s) détectée(s)."
    printf '%s\n' "$report" | grep -E 'CRITICAL|Library|Total' | head -15
    echo "Qualifie chaque résultat (exploitable ici ?) et propose la remédiation minimale."
  } >&2
  exit 2
fi
exit 0
