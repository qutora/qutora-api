#!/bin/bash
# Initialize SQL Server and create QutoraDB database

# Start SQL Server in the background
/opt/mssql/bin/sqlservr &

# Wait for SQL Server to start (max 60 seconds)
echo "Waiting for SQL Server to start..."
for i in {1..60}; do
    if /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" &> /dev/null; then
        echo "SQL Server is ready!"
        break
    fi
    echo "Waiting... ($i/60)"
    sleep 1
done

# Create QutoraDB database if it doesn't exist
echo "Creating QutoraDB database..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'QutoraDB')
BEGIN
    CREATE DATABASE QutoraDB;
    PRINT 'QutoraDB created successfully';
END
ELSE
BEGIN
    PRINT 'QutoraDB already exists';
END
"

echo "Database initialization complete!"

# Wait for SQL Server process (keep container running)
wait

