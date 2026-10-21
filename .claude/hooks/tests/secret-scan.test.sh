#!/usr/bin/env bash
# Tests du hook secret-scan.sh, dans un dépôt Git jetable.
#   bash .claude/hooks/tests/secret-scan.test.sh        → code de sortie 0 si tous les cas passent
#
# Les mots-clés sensibles sont ASSEMBLÉS à l'exécution ("pass""word") : ce fichier ne contient
# aucun motif de secret en clair, sinon secret-scan bloquerait son propre commit. Les valeurs
# sont fictives.
set -uo pipefail

HOOK="$(cd "$(dirname "$0")/.." && pwd)/secret-scan.sh"
P="pass""word"; T="to""ken"; S="sec""ret"; K="api""Key"
R="$(mktemp -d)"
trap 'cd /; rm -rf "$R"' EXIT
cd "$R" || exit 1
git init -q; git config core.autocrlf false

ok=0; ko=0
t() {   # t <fichier> <contenu> <code attendu : 0 = accepté, 2 = bloqué> <libellé>
  git rm -rq --cached . 2>/dev/null; rm -f ./*.* .env* 2>/dev/null
  printf '%s\n' "$2" > "$1"; git add -f "$1"
  jq -n --arg c "$R" '{cwd:$c, tool_input:{command:"git commit -m t"}}' | bash "$HOOK" >/dev/null 2>&1
  local r=$?
  if [ "$r" = "$3" ]; then ok=$((ok+1)); else ko=$((ko+1)); echo "ÉCHEC : $4 (exit=$r, attendu $3)"; fi
}

# Vrais secrets : doivent être BLOQUÉS.
t app.json  "\"catalogdb\": \"Host=db;$P=Sup3rS3cret!\""      2 "mot de passe dans une chaîne de connexion"
t cfg.yml   "$P: hunter22"                                       2 "mot de passe YAML"
t cfg.yml   "$P: example123value"                               2 "valeur contenant le mot example"
t pod.yaml  "  $T: falsehood-x9"                                2 "valeur commençant par false"
t cfg.ini   "$P=on-xyz123"                                      2 "valeur commençant par on-"
t cfg.ini   "$T=true-abc987"                                    2 "valeur commençant par true-"
t cfg.yml   "$S: no-way-9z"                                     2 "valeur commençant par no-"
t .env      "X=1"                                               2 "vrai fichier .env"
t id.pem    "x"                                                 2 "fichier .pem"

# Contournements : un secret à côté d'un booléen ou d'un placeholder reste BLOQUÉ.
t cfg.json  "{\"$P\":\"abc123\",\"$T\":false}"                  2 "secret à côté d'un booléen"
t cfg.json  "{\"$P\":\"Sup3rS3cret!\",\"url\":\"https://example.com\"}" 2 "secret à côté de example"

# Faux positifs : doivent PASSER.
t pod.yaml  "      automountServiceAccount${T^}: false"         0 "réglage Kubernetes à false"
t pod.yaml  "      automountServiceAccount${T^}: false   # c"    0 "réglage à false + commentaire"
t cfg.json  "\"$K\": null"                                      0 "valeur null"
t cfg.json  "{\"$T\":false}"                                    0 "booléen suivi d'une accolade"
t cfg.json  "[\"$P\":null]"                                     0 "null suivi d'un crochet"
t cfg.ini   "$T=on;"                                            0 "booléen suivi d'un point-virgule"
t compose.yml "${P^}=\${CATALOG_DB_${P^^}:?msg}"                0 "référence \${VAR:?msg}"
t cfg.yml   "$P: changeme"                                      0 "placeholder changeme"
t .env.example "CATALOG_DB_${P^^}=<a-generer-localement>"       0 "modèle .env.example"

echo "secret-scan : $ok réussis, $ko en échec"
[ "$ko" -eq 0 ]
