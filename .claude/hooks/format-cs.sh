#!/usr/bin/env bash
# Hook PostToolUse (Write|Edit) — formate le fichier C# que l'agent vient d'écrire.
#
# Pourquoi un hook et pas une consigne dans CLAUDE.md : le formatage doit être appliqué
# à CHAQUE édition, par tout le monde, sans dépendre de la mémoire du modèle.
# Ne bloque jamais : un formatage raté ne doit pas faire échouer l'édition.
set -uo pipefail

# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n.
file="$(jq -r '.tool_input.file_path // empty' 2>/dev/null | tr -d '\r')"
case "$file" in *.cs) ;; *) exit 0 ;; esac

# Sous Windows (Git Bash), Claude Code transmet un chemin C:\... : on le convertit en /c/...
# pour pouvoir le rendre relatif à $PWD ci-dessous.
command -v cygpath >/dev/null 2>&1 && file="$(cygpath -u "$file")"
[ -f "$file" ] || exit 0

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0
sln="$(ls ./*.slnx ./*.sln 2>/dev/null | head -1)"
[ -n "$sln" ] || exit 0

# `dotnet format --include` attend un chemin RELATIF à la racine du dépôt.
rel="${file#"$PWD"/}"
dotnet format whitespace "$sln" --include "$rel" --verbosity quiet >/dev/null 2>&1 || true
exit 0
