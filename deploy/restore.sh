#!/usr/bin/env bash
# Restore the database (and optionally uploads) from a backup in the "backups" volume.
#   ./restore.sh                                   lists the backups
#   ./restore.sh MadrasaDb_20261123-0200.bak.gz    restores that database backup (asks first)
#   ./restore.sh MadrasaDb_20261123-0200.bak.gz uploads_20261123-0200.tar.gz
set -euo pipefail
cd "$(dirname "$0")"
source .env

if [[ $# -lt 1 ]]; then
  docker compose exec backup ls -lh /backups
  exit 0
fi
BAK="$1"; UPL="${2:-}"
read -r -p "This replaces the live database with ${BAK}. Type RESTORE to continue: " ok
[[ "$ok" == "RESTORE" ]] || { echo "Cancelled."; exit 1; }

docker compose stop api
docker compose exec backup bash -c "cd /backups && gunzip -kf '${BAK}' && chmod a+r '${BAK%.gz}'"
docker compose exec backup /opt/mssql-tools18/bin/sqlcmd -C -S db -U sa -P "$SA_PASSWORD" -b -Q \
  "ALTER DATABASE [MadrasaDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   RESTORE DATABASE [MadrasaDb] FROM DISK = N'/var/opt/mssql/backup/${BAK%.gz}' WITH REPLACE, CHECKSUM;
   ALTER DATABASE [MadrasaDb] SET MULTI_USER;"
docker compose exec backup rm -f "/backups/${BAK%.gz}"
if [[ -n "$UPL" ]]; then
  docker compose run --rm --no-deps -v madrasa_uploads:/restore --entrypoint bash backup -c "rm -rf /restore/* && tar -xzf '/backups/${UPL}' -C /restore"
fi
docker compose start api
echo "Restored. The API is starting again."
