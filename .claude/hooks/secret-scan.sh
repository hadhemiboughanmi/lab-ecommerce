#!/usr/bin/env bash
# Hook PreToolUse (Bash) — bloque un git commit dont le diff contient un secret probable.
set -uo pipefail

input="$(cat)"

# Sans jq, la commande ne peut pas être lue : on refuse le commit plutôt que de le laisser
# passer en silence (un garde-fou qui échoue doit échouer fermé).
if ! command -v jq >/dev/null 2>&1; then
  if printf '%s' "$input" | grep -qE 'git[^"]*commit'; then
    echo "BLOQUÉ par le hook secret-scan : jq est introuvable, le diff ne peut pas être vérifié." >&2
    echo "Demande à l'utilisateur d'installer jq (winget install jqlang.jq), puis relance le commit." >&2
    exit 2
  fi
  exit 0
fi

# tr -d '\r' : sous Windows, jq termine sa sortie par \r\n.
cmd="$(printf '%s' "$input" | jq -r '.tool_input.command // empty' 2>/dev/null | tr -d '\r')" || exit 0
[ -z "$cmd" ] && exit 0
printf '%s' "$cmd" | grep -qE '\bgit\b.*\bcommit\b' || exit 0

dir="$(printf '%s' "$input" | jq -r '.cwd // empty' 2>/dev/null | tr -d '\r')"
cd "${dir:-${CLAUDE_PROJECT_DIR:-.}}" 2>/dev/null || exit 0
git rev-parse --git-dir >/dev/null 2>&1 || exit 0

# La commande peut indexer elle-même ses fichiers (git add … && git commit, git commit -a) :
# au moment du hook, ils ne sont pas encore dans l'index, il faut donc aussi lire l'arbre de travail.
scan_worktree=false
if printf '%s' "$cmd" | grep -qE '\bgit[[:space:]]+add\b|\bcommit\b.*[[:space:]]-[[:alpha:]]*a|--all\b'; then
  scan_worktree=true
fi

# Lignes ajoutées, au format "fichier<TAB>ligne".
added_lines() {
  awk '/^\+\+\+ / { file = substr($0, 7); next }
       /^\+/      { print file "\t" substr($0, 2) }'
}

{
  git diff --cached -U0 --no-color | added_lines
  if $scan_worktree; then
    git diff -U0 --no-color | added_lines
    git ls-files --others --exclude-standard -z | while IFS= read -r -d '' f; do
      grep -Iq . "$f" 2>/dev/null && sed "s|^|$f\t|" "$f"
    done
  fi
} > "${TMPDIR:-/tmp}/secret-scan.$$"
trap 'rm -f "${TMPDIR:-/tmp}/secret-scan.$$"' EXIT

found=""

# 1. gitleaks sur l'index s'il est installé (code 1 = fuite trouvée).
if command -v gitleaks >/dev/null 2>&1; then
  gitleaks protect --staged --redact --no-banner >/dev/null 2>&1
  [ $? -eq 1 ] && found="$(git diff --cached --name-only) (détecté par gitleaks)"
fi

# 2. Motifs : clé sensible suivie d'une valeur littérale. Sert de repli sans gitleaks,
#    et couvre toujours les fichiers que la commande va indexer elle-même.
#    Une valeur qui commence par $ ou ? est une RÉFÉRENCE, pas un secret : ${VAR}, $(commande),
#    ou le ${VAR:?message} de compose (qui exige la variable).
#    LIMITE CONNUE : un vrai mot de passe qui commencerait par $ ou ? n'est plus détecté ici.
#    gitleaks (utilisé s'il est installé) n'a pas cette limite.
RE="(password|passwd|pwd|secret|token|api[_-]?key|connectionstring)[\"']?[[:space:]]*[:=][[:space:]]*[\"']?[^\"'[:space:],;{}\$?][^\"'[:space:],;{}]{3,}"
KEY='BEGIN (RSA |OPENSSH |EC |DSA )?PRIVATE KEY'
# Valeurs qui ne sont jamais des secrets, EFFACÉES de la ligne avant la recherche (et non pas
# la ligne entière écartée : sinon `"token":false` ou `example` placé à côté d'un vrai mot de
# passe suffirait à le cacher) :
#   - valeurs factices : ${VAR}, <placeholder>, changeme, example ;
#   - valeurs booléennes ou vides, par exemple le réglage Kubernetes automountServiceAccountToken
#     mis à false (il retire aux pods leur jeton d'accès à l'API).
PLACEHOLDER='\$\{[A-Za-z_]+\}|<[a-z-]+>|changeme|example'
BOOLEAN="(password|passwd|pwd|secret|token|api[_-]?key|connectionstring)[\"']?[[:space:]]*[:=][[:space:]]*[\"']?(true|false|null|yes|no|on|off)\\b[\"']?"
if ! command -v gitleaks >/dev/null 2>&1 || $scan_worktree; then
  scan="${TMPDIR:-/tmp}/secret-scan.$$"
  # Nettoyage du contenu seul (colonne 2), le nom de fichier (colonne 1) reste intact.
  cleaned="$(paste <(cut -f1 "$scan") <(cut -f2- "$scan" | sed -E "s/$BOOLEAN//Ig; s/$PLACEHOLDER//Ig"))"
  hits="$(printf '%s\n' "$cleaned" | grep -iE "$RE|$KEY" | cut -f1 | sort -u)"
  [ -n "$hits" ] && found="$found"$'\n'"$hits"
fi

# 3. Fichiers sensibles par leur nom, quel que soit leur contenu.
names="$(git diff --cached --name-only; $scan_worktree && git ls-files --others --exclude-standard)"
# Les MODÈLES versionnés exprès (.env.example, .env.sample, .env.template) ne contiennent pas de
# valeur : leur contenu reste analysé par les motifs ci-dessus, seul leur nom est accepté.
bad_names="$(printf '%s\n' "$names" | grep -iE '(^|/)\.env(\..*)?$|\.(pem|key|pfx|p12)$' \
              | grep -viE '(^|/)\.env\.(example|sample|template)$' | sort -u)"
[ -n "$bad_names" ] && found="$found"$'\n'"$bad_names"

found="$(printf '%s\n' "$found" | sed '/^$/d' | sort -u)"
[ -z "$found" ] && exit 0

# La valeur du secret n'est jamais réécrite ici : seuls les fichiers sont nommés.
{
  echo "BLOQUÉ par le hook secret-scan : le commit contient un secret probable."
  echo "Fichiers concernés :"
  printf '%s\n' "$found" | sed 's/^/  - /'
  echo "Pour sortir le secret du code :"
  echo "  1. Retire la valeur du fichier (git restore --staged <fichier>, puis édite-le)."
  echo "  2. Stocke-la hors du dépôt : dotnet user-secrets set \"ConnectionStrings:<nom>\" \"<valeur>\" --project <projet>,"
  echo "     ou une variable d'environnement (ConnectionStrings__<nom>)."
  echo "  3. Si la valeur a déjà été committée ou partagée, elle est compromise : elle doit être révoquée."
  echo "Ne cherche pas à contourner ce hook : présente ces étapes à l'utilisateur."
} >&2
exit 2
