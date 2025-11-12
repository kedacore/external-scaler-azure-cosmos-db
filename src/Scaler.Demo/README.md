# KEDA External Scaler for Azure Cosmos DB

This directory contains the source code for two sample projects to demonstrate the functionality of the external scaler.

- `Keda.CosmosDb.Scaler.Demo.OrderGenerator` - used to push fake purchase orders to a test Cosmos DB container.
- `Keda.CosmosDb.Scaler.Demo.OrderProcessor` - used to read these orders from the container and process them.

We will later deploy the order-processor application to Kubernetes cluster and use KEDA along with the external scaler from `Keda.CosmosDb.Scaler` project to scale the application.

## Prerequisites

- [Azure Cosmos DB account](https://azure.microsoft.com/free/cosmos-db/)
- [Docker Hub account](https://hub.docker.com/signup)
- Kubernetes cluster
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

## Testing sample application locally on Docker

> Note: Connection String is used to run the application locally on Docker, as Managed Identity is not available locally.

1. Open command prompt or shell and change to the root directory of the cloned repo.

1. Run the below commands to build the Docker container images for order-generator and order-processor applications.

    ```text
    # docker build --file .\src\Scaler.Demo\OrderGenerator\Dockerfile --force-rm --tag cosmosdb-order-generator .
    # docker build --file .\src\Scaler.Demo\OrderProcessor\Dockerfile --force-rm --tag cosmosdb-order-processor .
    ```

2. Create test-database and test-container within the database in Cosmos DB account by running the order-generator application inside the container with `setup` option. Make sure to put the connection string of Cosmos DB account in the command below.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --interactive --rm --tty cosmosdb-order-generator setup
    ```

    > **Caution** The default application settings provision a throughput of 11,000 RU/s to the test Cosmos DB container. This sets the number of its [physical partitions](https://docs.microsoft.com/azure/cosmos-db/partitioning-overview#physical-partitions) to 2. If you have a free Azure account which offers limited throughput, or if you want to limit the cost of running the sample, be sure to update the throughput to 400 RU/s by setting the value of property `CosmosDb:ConnectionThroughput` to `400` in file `src/Scaler.Demo/OrderGenerator/appsettings.json` before building the container image. That would still allow testing of KEDA scaling between 0 and 1 instances but not upto 2 instances. Also, at any point, you can run order-generator with `teardown` option to delete the database and Cosmos DB container inside.

1. Start a second shell instance and run the order-processor application in a new container. You can put the same connection string in both places in the command below. Note that the sample applications are written to handle different Cosmos DB accounts for monitored and lease containers but having two different accounts is not a requirement.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --env CosmosDbConfig__LeaseConnection="<connection-string>" --interactive --rm --tty cosmosdb-order-processor
    ```

    The order-processor application will create lease database and container if they do not exist. The default application settings would share the same database between the monitored and lease containers. The order-processor application will then activate a change-feed processor to monitor and process new changes in the monitored container.

1. Keep the order-processor application running. Go back to the first shell instance and run order-generator application with `generate` option to add fake orders to the test Cosmos DB container.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --interactive --tty --rm cosmosdb-order-generator generate
    Let's queue some orders, how many do you want? 3
    Do you want to limit orders to single article (to put them in a single partition)? (Y/N) N
    Creating order 1f157b6e-c51f-4e02-a492-295e7fd47fbe - 3 unit(s) of Mouse for Cleta Padberg
    Creating order ffeb4822-43e6-4b80-a439-d2ad09a278c1 - 9 unit(s) of Soap for Uriel Cormier
    Creating order b0e396d7-e140-43f5-872c-f47df6bccf2e - 4 unit(s) of Chair for Granville Auer
    That's it, see you later!
    ```

1. Go back to the second shell where the order-processor application is running. Check the console output and verify that the orders were processed.

    ```text
    2021-09-03 06:52:34 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Started change feed processor instance Instance-588a09449616
    2021-09-03 06:52:34 info: Microsoft.Hosting.Lifetime[0]
        Application started. Press Ctrl+C to shut down.
    2021-09-03 06:52:34 info: Microsoft.Hosting.Lifetime[0]
        Hosting environment: Production
    2021-09-03 06:52:34 info: Microsoft.Hosting.Lifetime[0]
        Content root path: /app
    2021-09-03 06:52:59 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        1 order(s) received
    2021-09-03 06:52:59 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Processing order e5912b0a-939b-45bc-87de-9baa314ca65e - 9 unit(s) of Soap bought by Uriel Cormier
    2021-09-03 06:53:00 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        2 order(s) received
    2021-09-03 06:53:00 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Processing order 6daae206-027c-4055-8d39-97274de1ab59 - 3 unit(s) of Mouse bought by Cleta Padberg
    2021-09-03 06:53:01 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Order e5912b0a-939b-45bc-87de-9baa314ca65e processed
    2021-09-03 06:53:02 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Order 6daae206-027c-4055-8d39-97274de1ab59 processed
    2021-09-03 06:53:02 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Processing order 135e1fb9-807a-489e-90f0-0cb4c3768a36 - 4 unit(s) of Chair bought by Granville Auer
    2021-09-03 06:53:04 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Order 135e1fb9-807a-489e-90f0-0cb4c3768a36 processed
    ```

7. Stop order-processor container from the first shell.

    ```text
    # docker container ls --no-trunc --format "{{.Image}} {{.Names}}"
    cosmosdb-order-processor jolly_wilbur
    # docker container stop jolly_wilbur
    jolly_wilbur
    ```

## Deploying KEDA and external scaler to cluster

1. Follow one of the steps on [Deploying KEDA](https://keda.sh/docs/deploy/) documentation page to deploy KEDA on your Kubernetes cluster.

1. **If using MI:**

    >[!NOTE]
    > Guided tutorial here: [Integrate KEDA with AKS](https://learn.microsoft.com/en-us/azure/azure-monitor/containers/integrate-keda)

    a. Enable workload identity  
    ```text
        # az aks update -n <cluster-name> -g <resource-group-name> --enable-oidc-issuer --enable-workload-identity 
    ```
    b. Create User Assigned Identity
    ```text
        # az identity create --name <identity-name> --resource-group <resource-group-name> --location <location-name> 
    ```
    c. Follow the tutorial to grant MI both data-plane and control-pane access to the Cosmos DB: [Connect to Azure Cosmos DB using RBAC](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-connect-role-based-access-control?pivots=azure-portal).

    d. [Create Service Account yaml](https://aka.ms/sa-aks-label)

    e. Create federated identity credential 
    ```text
        # az identity federated-credential create --name <credential-name> --resource-group <resource-group-name> --identity-name <identity-name> --issuer <oidc-issuer-url> --subject system:serviceaccount:<namespace>:<serviceaccountname>
    ```

2. Open command prompt or shell and change to the root directory of the cloned repo.

3. Build container image for the external scaler and push the image to Docker Hub. Make sure to replace `<docker-id>` in below commands with your Docker ID.

    ```text
    # docker build --file .\src\Scaler\Dockerfile --force-rm --tag cosmosdb-scaler .
    # docker login --username <docker-id>
    # docker tag cosmosdb-scaler:latest <docker-id>/cosmosdb-scaler:latest
    # docker push <docker-id>/cosmosdb-scaler:latest
    ```

4. Update your Docker ID in the image path in manifest file `src/Scaler/deploy.yaml` and apply it to deploy the external scaler application. </br>
   **If using MI**: *Add your service account name, and enable using workload identity in the manifest file as well.*

    ```text
    kubectl apply --filename=src/Scaler/deploy.yaml
    ```

## Deploying sample application to cluster

1. Build the Docker container image for order-processor application (if you haven't already) and push the container image to Docker Hub. Make sure to replace `<docker-id>` in below commands with your Docker ID.

    ```text
    # docker build --file .\src\Scaler.Demo\OrderProcessor\Dockerfile --force-rm --tag cosmosdb-order-processor .
    # docker tag cosmosdb-order-processor:latest <docker-id>/cosmosdb-order-processor:latest
    # docker push <docker-id>/cosmosdb-order-processor:latest
    ```

2. Update your Docker ID in the image path in manifest file `src/Scaler.Demo/OrderProcessor/deploy.yaml`. Also, update the values of connection strings (or account endpoint and client ID, if using managed identity) to point to the test Cosmos DB account. Apply the manifest to deploy the order-processor application.

    ```text
    kubectl apply --filename=src/Scaler.Demo/OrderProcessor/deploy.yaml
    ```

3. Ensure that the order-processor application is running correctly on the cluster by checking application logs. The application will create lease database and container if they do not exist, hence it is needed to run for a few seconds before we enable auto-scaling for order-processor, as that would immediately bring replicas to 0 if there are no orders pending to be processed.

    ```text
    # kubectl get pods
    NAME                                       READY   STATUS    RESTARTS   AGE
    cosmosdb-order-processor-b59956989-bcbzg   1/1     Running   0          50s
    cosmosdb-scaler-64dd48678c-zjcgd           1/1     Running   0          23m
    # kubectl logs cosmosdb-order-processor-b59956989-bcbzg
    2021-09-03 08:05:01 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Started change feed processor instance Instance-cosmosdb-order-processor-b59956989-bcbzg
    ...
    ```

4. Apply the manifest file for scaled object, `src/Scaler.Demo/OrderProcessor/deploy-scaledobject.yaml`

    ```text
    kubectl apply --filename=src/Scaler.Demo/OrderProcessor/deploy-scaledobject.yaml
    ```

    > **Note** Ideally, we would have created `TriggerAuthentication` resource that would enable sharing of the connection strings as secrets between the scaled object and the target application. However, this is not possible since at the moment, the triggers of `external` type do not support referencing a `TriggerAuthentication` resource ([link](https://keda.sh/docs/scalers/external/#authentication-parameters)).

## Testing auto-scaling for sample application

1. Verify that there are no pods for order-processor running after the scaled object was created.

    ```text
    # kubectl get pods
    NAME                               READY   STATUS    RESTARTS   AGE
    cosmosdb-scaler-64dd48678c-d6dqq   1/1     Running   0          10m
    ```

1. Add new orders to the Cosmos DB container by running the order-generator. Select option to generate all orders for the same article.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --interactive --tty --rm cosmosdb-order-generator generate
    Let's queue some orders, how many do you want? 150
    Do you want to limit orders to single article (to put them in a single partition)? (Y/N) Y
    ...
    ```

    This would restrict the orders to a single logical partition (and a single physical partition thereof). The external scaler scales the targets according to the number of change feeds that have non-zero pending messages remaining to be processed. The total number of change feeds (with or without pending messages) equals the number of physical partitions in the Cosmos DB container.

1. Verify that only one pod is created for the order-processor. It may take a few seconds for the pod to show up.

    ```text
    # kubectl get pods
    NAME                                       READY   STATUS    RESTARTS   AGE
    cosmosdb-order-processor-b59956989-fscsb   1/1     Running   0          30s
    cosmosdb-scaler-64dd48678c-d6dqq           1/1     Running   0          20m
    ```

1. Now, add more orders to the Cosmos DB container but this time, select option to generate orders of different articles.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --interactive --tty --rm cosmosdb-order-generator generate
    Let's queue some orders, how many do you want? 250
    Do you want to limit orders to single article (to put them in a single partition)? (Y/N) N
    ...
    ```

1. Verify that two pods are created for the order-processor.

    ```text
    NAME                                       READY   STATUS    RESTARTS   AGE
    cosmosdb-order-processor-b59956989-88dc5   1/1     Running   0          15s
    cosmosdb-order-processor-b59956989-dxp4b   1/1     Running   0          3s
    cosmosdb-scaler-64dd48678c-d6dqq           1/1     Running   0          35m

    The external scaler scales the targets according to the number of change feeds that have non-zero pending messages remaining to be processed. The total number of change feeds (with or without pending messages) equals the number of physical partitions in the Cosmos DB container.
    ```

1. You can also verify that both order-processor pods are able to share the processing of orders.

    ```text
    # kubectl logs cosmosdb-order-processor-b59956989-dxp4b --tail=4
    2021-09-03 12:57:41 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Order 5ba7f503-0185-49f6-9fce-3da999464049 processed
    2021-09-03 12:57:41 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Processing order ce1f05ad-08ff-4535-858f-3158de41971b - 8 unit(s) of Computer bought by Jaren Tremblay

    # kubectl logs cosmosdb-order-processor-b59956989-88dc5 --tail=4
    2021-09-03 12:57:53 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Order e881c998-1318-411e-8181-fa638335910e processed
    2021-09-03 12:57:53 info: Keda.CosmosDb.Scaler.Demo.OrderProcessor.Worker[0]
        Processing order ca17597f-7aa2-4b04-abd8-724139b2c370 - 1 unit(s) of Gloves bought by Donny Shanahan
    ```

1. To verify that the scale down is working, check that the cosmosdb-order-processor pod/s aren't running anymore after all orders are done processing. The default `cooldownPeriod` is 5 minutes, after which the cosmosdb-order-processor will stop running.

## Cleaning sample application from cluster

1. Delete the scaled object and order-processor application.

    ```text
    # kubectl delete scaledobject cosmosdb-order-processor-scaledobject
    # kubectl delete deployment cosmosdb-order-processor
    ```

2. Optionally, delete the external scaler and KEDA from cluster. The following commands assume that KEDA was installed with Helm.

    ```text
    # kubectl delete service cosmosdb-scaler
    # kubectl delete deployment cosmosdb-scaler
    # helm uninstall keda --namespace keda
    # kubectl delete namespace keda
    ```

3. The monitored container can be deleted with the below command. The lease container can be deleted on Azure Portal.

    ```text
    # docker run --env CosmosDbConfig__Connection="<connection-string>" --interactive --rm --tty cosmosdb-order-generator teardown
    ```
