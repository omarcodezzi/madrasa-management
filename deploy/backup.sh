#!/usr/bin/env bash
# Nightly backup, run by the "backup" service: a full SQL Server backup (checked with RESTORE VERIFYONLY)
# plus a tarball of uploaded photos, in the "backups" volume. Old files are removed after RETAIN_DAYS.
#   Run one now:  docker compose exec backup /bin/bash /backup.sh --now
set -uo pipefail
# SQL Server (uid 10001) writes the .bak files into this shared volume
chown 10001:0 /backups 2>/dev/null || true
umask 077
SQL=/opt/mssql-tools18/bin/sqlcmd
DB=MadrasaDb

run_backup() {
  local stamp file
  stamp=$(date +%Y%m%d-%H%M)
  file="${DB}_${stamp}.bak"
  echo "[$(date '+%F %T')] backing up ${DB} -> ${file}"
  # The server writes the file to /var/opt/mssql/backup, which is this container's /backups
  if ! "$SQL" -C -S db -U sa -P "$SA_PASSWORD" -b -Q \
      "BACKUP DATABASE [${DB}] TO DISK = N'/var/opt/mssql/backup/${file}' WITH INIT, CHECKSUM, NAME = N'${DB} ${stamp}';
       RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/backup/${file}' WITH CHECKSUM;"; then
    echo "[$(date '+%F %T')] DATABASE BACKUP FAILED" >&2
    return 1
  fi
  gzip -f "/backups/${file}"
  tar -czf "/backups/uploads_${stamp}.tar.gz" -C /uploads . 2>/dev/null || echo "uploads archive failed" >&2
  find /backups -maxdepth 1 -type f \( -name "${DB}_*.bak.gz" -o -name 'uploads_*.tar.gz' \) -mtime "+${RETAIN_DAYS}" -delete
  echo "[$(date '+%F %T')] done: $(ls -1 /backups | wc -l) files kept"
}

if [[ "${1:-}" == "--now" ]]; then run_backup; exit $?; fi

echo "Backup job started: every day at ${BACKUP_HOUR}:00 (${TZ:-UTC}), keeping ${RETAIN_DAYS} days"
last=""
while true; do
  today=$(date +%F)
  if [[ "$(date +%-H)" -ge "${BACKUP_HOUR}" && "$last" != "$today" ]]; then
    run_backup && last="$today" || sleep 600   # retry in 10 minutes on failure
  fi
  sleep 300
done
