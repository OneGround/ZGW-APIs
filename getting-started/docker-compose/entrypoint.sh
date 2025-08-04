#!/bin/sh

# Exit immediately if a command exits with a non-zero status.
set -e

# Update the trusted CA certificates in the container.
echo "Updating CA certificates..."
update-ca-certificates

# Execute the command passed as arguments to this script.
exec "$@"