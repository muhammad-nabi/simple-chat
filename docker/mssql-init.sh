#!/bin/bash
# MSSQL initialization script
# Waits for SQL Server to start, then caps max server memory at 512MB.
# Run in background alongside SQL Server (see docker-compose.yml command).

echo "mssql-init: Waiting for SQL Server to start..."

# Wait for SQL Server to accept connections (up to 60 seconds)
for i in $(seq 1 60); do
    /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -No \
        -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "mssql-init: SQL Server is ready."
        break
    fi
    if [ "$i" -eq 60 ]; then
        echo "mssql-init: ERROR - SQL Server did not start within 60 seconds. Memory cap not applied."
        exit 1
    fi
    echo "mssql-init: Waiting... ($i/60)"
    sleep 1
done

# Configure max server memory to 512MB
echo "mssql-init: Setting max server memory to 512MB..."
/opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -No \
    -Q "
    EXEC sp_configure 'show advanced options', 1;
    RECONFIGURE;
    EXEC sp_configure 'max server memory (MB)', 512;
    RECONFIGURE;
    "

if [ $? -eq 0 ]; then
    echo "mssql-init: Max server memory set to 512MB successfully."
else
    echo "mssql-init: WARNING - Failed to set max server memory."
fi
