#!/usr/bin/env bash
# Regenerates database/MMS.sql (create database + all tables + reporting objects) and database/schema.sql
# from the EF Core migrations. Run after adding a migration:  ./database/build.sh
set -euo pipefail
cd "$(dirname "$0")/../backend"
dotnet tool restore >/dev/null
EF="dotnet ef migrations script -p src/Madrasa.Infrastructure -s src/Madrasa.Infrastructure --idempotent"
$EF -o ../database/schema.sql
$EF -o /tmp/mms-body.sql
{
  cat ../database/MMS.header.sql
  sed '1s/^\xEF\xBB\xBF//' /tmp/mms-body.sql   # drop the UTF-8 BOM EF writes
} > ../database/MMS.sql
rm -f /tmp/mms-body.sql
echo "database/MMS.sql and database/schema.sql updated"
