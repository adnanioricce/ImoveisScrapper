# Use the official PostgreSQL image from Docker Hub
FROM postgres:latest

# Set environment variables for PostgreSQL
ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=mysecretpassword
ENV POSTGRES_DB=realestate

# Copy the SQL initialization script into the Docker container
COPY init-db.sql /docker-entrypoint-initdb.d/

# Expose the PostgreSQL port
EXPOSE 5432
