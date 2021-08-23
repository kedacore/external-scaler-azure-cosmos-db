# KEDA External Scaler for Azure Cosmos DB

## Testing Scaler

### Build Docker Image
The instructions assume that you have cloned this repo, have [Docker](https://www.docker.com/products/docker) installed, and have a command prompt or shell opened within the root directory.

1. Ensure that there are no untracked files in the repo: `git clean -xdf`.
1. Build the container image: `docker image build . --tag cosmos-db-scaler --file .\Keda.CosmosDbScaler.Demo.OrderProcessor\Dockerfile`.
1. Verify that application can start with errors: `docker run --rm cosmos-db-scaler`.

### Publish Image to Docker Hub
You will need to have a Docker ID to be able to push the image to Docker Hub. If not, create a one by signing up on [Docker Hub](https://hub.docker.com/signup).

1. Login to Docker Hub: `docker login --username <docker-id>`.
1. Tag the locally created image with Docker ID: `docker tag cosmos-db-scaler:latest <docker-id>/cosmos-db-scaler:latest`.
1. Push the image: `docker push <docker-id>/cosmos-db-scaler:latest`.

### Prepare Kubernetes Cluster Environment
The instructions assume that you have Kubernetes cluster available and have [kubectl](https://kubernetes.io/docs/tasks/tools/) installed.

1. Follow one of the steps on [Deploying KEDA](https://keda.sh/docs/deploy/) documentation page to deploy KEDA.
1. Update your docker ID in `kubernetes.yaml` file.
1. Create deployment for Cosmos DB scaler: `kubectl apply --filename=kubernetes.yaml`.
