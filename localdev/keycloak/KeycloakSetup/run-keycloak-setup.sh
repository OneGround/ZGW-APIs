#!/bin/bash

echo "Starting Keycloak Setup Tool..."
echo

# Build the project
echo "Building project..."
dotnet build

# Check if build was successful
if [ $? -ne 0 ]; then
    echo "Build failed. Please check the errors above."
    exit 1
fi

echo "Build successful. Running setup..."
echo

# Run the setup tool
dotnet run

# Check if setup was successful
if [ $? -ne 0 ]; then
    echo "Setup failed. Please check the errors above."
    exit 1
fi

echo
echo "Setup completed successfully!" 