#!/usr/bin/env bash
# Hook PreToolUse (Write|Edit|MultiEdit) — une migration EF Core déjà committée est immuable.
#
# Une migration partagée a peut-être déjà été appliquée sur la base d'un collègue, d'une CI
# ou d'un environnement : la réécrire désynchronise ces bases sans bruit. La seule
# correction sûre est une NOUVELLE migration. Ce hook l'impose, quel que soit le prompt.
set -uo pipefail

# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n.
file="$(jq -r '.tool_input.file_path // empty' 2>/dev/null | tr -d '\r')"
# Sous Windows (Git Bash), Claude Code transmet un chemin C:\... : on le convertit en /c/...
# pour que les motifs */Migrations/* ci-dessous correspondent.
command -v cygpath >/dev/null 2>&1 && [ -n "$file" ] && file="$(cygpath -u "$file")"

case "$file" in
  */Migrations/*ModelSnapshot.cs) exit 0 ;;   # régénéré par dotnet ef, jamais édité à la main
  */Migrations/*.cs) ;;
  *) exit 0 ;;
esac

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0

# Fichier encore jamais committé : migration en cours d'écriture, on laisse faire.
git ls-files --error-unmatch "$file" >/dev/null 2>&1 || exit 0

{
  echo "BLOQUÉ par le hook guard-migrations : $(basename "$file") est déjà committée."
  echo "Une migration partagée ne se modifie pas : elle a pu être appliquée ailleurs."
  echo "Procédure : dotnet ef migrations add <NomDuCorrectif> --project src/ECommerce.Catalog.Api"
} >&2
exit 2
