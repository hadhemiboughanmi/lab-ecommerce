#!/usr/bin/env bash
# Hook PreToolUse (Bash) du sous-agent k8s-debug-pod — LECTURE SEULE IMPOSÉE.
#
# Le sous-agent a besoin de Bash pour lancer kubectl, mais Bash permet tout. Sans ce hook, sa
# « lecture seule » ne serait qu'une consigne. Ici, toute commande est découpée en segments
# (&&, ||, ;, |) et CHAQUE segment doit figurer dans une liste blanche de lecture. Tout le reste
# est refusé (exit 2), y compris ce qui n'est pas prévu : on échoue fermé.
set -uo pipefail

input="$(cat)"
deny() {
  {
    echo "REFUSÉ par readonly-kubectl : $1"
    echo "Ce sous-agent est en LECTURE SEULE (get, describe, logs, top, events)."
    echo "Propose le correctif sous forme de diff ou de commande, sans l'exécuter : l'humain décidera."
  } >&2
  exit 2
}

command -v jq >/dev/null 2>&1 || deny "jq introuvable, la commande ne peut pas être vérifiée"
cmd="$(printf '%s' "$input" | jq -r '.tool_input.command // empty' 2>/dev/null | tr -d '\r')"
[ -n "$cmd" ] || exit 0

# Substitutions de commande et redirections vers un fichier : refusées d'emblée.
printf '%s' "$cmd" | grep -qE '\$\(|`' && deny "substitution de commande"
stripped="$(printf '%s' "$cmd" | sed -E 's/[0-9]?>&[0-9]//g; s#[0-9]?>[[:space:]]*/dev/null##g')"
printf '%s' "$stripped" | grep -q '>' && deny "redirection vers un fichier"

# Découpage en segments.
segments="$(printf '%s' "$stripped" | sed -E 's/(\|\||&&|;|\|)/\n/g')"

while IFS= read -r seg; do
  seg="$(printf '%s' "$seg" | sed -E 's/^[[:space:]]+|[[:space:]]+$//g')"
  [ -z "$seg" ] && continue
  first="${seg%% *}"
  case "$first" in
    kubectl|kubectl.exe)
      # Retire les options globales qui précèdent le verbe (-n x, --namespace x, --context x…).
      rest="$(printf '%s' "${seg#* }" | sed -E 's/(^|[[:space:]])(-n|--namespace|--context|--kubeconfig)([[:space:]]+|=)[^[:space:]]+//g; s/^[[:space:]]+//')"
      verb="${rest%% *}"
      case "$verb" in
        get)
          # Le CONTENU d'un Secret ne doit jamais sortir.
          if printf '%s' "$rest" | grep -qiE '(^|[[:space:]/])(secret|secrets)([[:space:]/.]|$)' \
             && printf '%s' "$rest" | grep -qE '(^|[[:space:]])(-o|--output)([[:space:]=]|[a-z])'; then
            deny "lecture du contenu d'un Secret"
          fi ;;
        describe|logs|top|events|explain|version|api-resources|cluster-info) ;;
        config)
          case "$rest" in
            "config current-context"*|"config get-contexts"*) ;;
            *) deny "kubectl $rest" ;;
          esac ;;
        auth)
          case "$rest" in "auth can-i"*) ;; *) deny "kubectl $rest" ;; esac ;;
        rollout)
          # Seules les sous-commandes de consultation : jamais undo, restart, pause, resume.
          case "$rest" in "rollout status"*|"rollout history"*) ;; *) deny "kubectl $rest" ;; esac ;;
        *) deny "kubectl $verb" ;;
      esac ;;
    docker|docker.exe)
      case "${seg#* }" in
        images*|"image ls"*|ps*|inspect*|version*) ;;
        *) deny "$seg" ;;
      esac ;;
    k3d|k3d.exe)
      case "${seg#* }" in
        "cluster list"*|version*) ;;
        *) deny "$seg" ;;
      esac ;;
    grep|egrep|head|tail|sort|uniq|wc|jq|cut|tr|awk|echo|cat|date|column) ;;
    sed)
      printf '%s' "$seg" | grep -qE '(^|[[:space:]])-[a-zA-Z]*i' && deny "sed -i (modification de fichier)" ;;
    *) deny "commande non autorisée : $first" ;;
  esac
done <<< "$segments"

exit 0
