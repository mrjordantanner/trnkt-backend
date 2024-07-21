# TRNKT Backend

## Overview

TRNKT is an NFT art gallery that consumes the OpenSea API to showcase a curated collection of NFTs. The backend is built using ASP.NET and DynamoDB to manage and store NFT data efficiently. This is an ongoing project, and contributions are welcome.

## Technologies Used

- **ASP.NET**: For building the backend API.
- **DynamoDB**: For storing and managing NFT data.
- **OpenSea API**: For fetching NFT metadata and details.

## Project Status

This project is currently in development. Below are the key features currently implemented:

- User registration and authentication.
- Fetching NFT data from the OpenSea API.
- Storing and retrieving NFT data using DynamoDB.
- Basic API endpoints for accessing NFT data.
- Custom Favorites Lists

## Getting Started

### Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download)
- [AWS CLI](https://aws.amazon.com/cli/)
- [DynamoDB Local](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.html) (for local development)

### Setup

1. **Clone the repository**:
    ```bash
    git clone https://github.com/yourusername/trnkt-backend.git
    cd trnkt-backend
    ```

2. **Restore dependencies**:
    ```bash
    dotnet restore
    ```

3. **Set up DynamoDB**:
   - If using DynamoDB Local, start it as per [official documentation](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/DynamoDBLocal.html).

4. **Run the application**:
    ```bash
    dotnet run
    ```

### Configuration

Configure the application settings in `appsettings.json` to connect to DynamoDB and the OpenSea API.

## Frontend Repository

The frontend of this project is built using React and TypeScript. You can find the frontend repository [here](https://github.com/mrjordantanner/trnkt-app).
