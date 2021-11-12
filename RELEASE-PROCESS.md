# Release Process

The release process of a new version of KEDA external scaler for Azure Cosmos DB involves the following:

## 1. Choose release title and tag name

Check the latest release on the [Releases](https://github.com/kedacore/keda-external-scaler-azure-cosmos-db/releases) page. Follow the [Semantic Versioning](https://semver.org/) guidelines for naming the new release. For instance, suppose that the latest release is **Version 1.2.0**, and that the next release is going to be a MINOR update, then its title would be **Version 1.3.0** and the associated tag name would be `v1.3.0`.

## 2. Create GitHub release

Open [New release](https://github.com/kedacore/keda-external-scaler-azure-cosmos-db/releases/new) page. Use the values for **Tag** and **Release title** as picked above. Include information on the changes that would get shipped with the new release inside **Release description** section. This includes new features, patches, breaking changes and deprecations. You can get list of all commits since the last release with a link similar to <https://github.com/kedacore/keda-external-scaler-azure-cosmos-db/compare/v0.1.0...main>. Do not include information that is not important to users. As an example, a change to issue template is not important.

Publish the release. This will trigger GitHub action to create a new Docker image from the tagged commit. After the action is executed, you should find a new Docker image with new tag is published on the [Packages](https://github.com/orgs/kedacore/packages?repo_name=keda-external-scaler-azure-cosmos-db) page.

## 3. Update Helm Chart

Once the GitHub release is created, update the Helm Chart for the external scaler in [kedacore/charts](https://github.com/kedacore/charts/tree/master/cosmosdb-scaler) repository. Depending on the changes that went in the release, this might just take updating the `version` and `appVersion` in [Chart.yaml](https://github.com/kedacore/charts/blob/master/cosmosdb-scaler/Chart.yaml) file, or on other hand, might involve adding and updating multiple template files.

Follow the [Contributing](https://github.com/kedacore/charts/blob/master/CONTRIBUTING.md) guide to create a pull request. After the pull request is completed, create a GitHub release in the repository following the same guide.
