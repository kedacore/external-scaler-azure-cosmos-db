# Release Process

The release process of a new version of KEDA external scaler for Azure Cosmos DB involves the following:

## 1. Choose release title and tag name

Check the latest release on [Releases](https://github.com/kedacore/external-scaler-azure-cosmos-db/releases) page. Follow the [Semantic Versioning](https://semver.org/) guidelines for naming the new release. For instance, suppose that the latest release is **Version 1.2.3**, and that the next release is going to be a MINOR update, then its title would be **Version 1.3.0** and the associated tag name would be `v1.3.0`.

## 2. Create GitHub release

Open [New release](https://github.com/kedacore/external-scaler-azure-cosmos-db/releases/new) page. Use the values for **Tag** and **Release title** as picked above. Include information on the changes that would get shipped with the new release inside **Release description** section. This includes new features, patches, breaking changes and deprecations. You can get list of all commits since the last release with a link similar to <https://github.com/kedacore/external-scaler-azure-cosmos-db/compare/v0.1.0...main>. Do not include information that is not important to users. As an example, a change to issue template is not important.

Publish the release. This will trigger GitHub action to create a new Docker image from the tagged commit. After the action is executed, you should find a new Docker image with new tag published on the [Packages](https://github.com/orgs/kedacore/packages?repo_name=external-scaler-azure-cosmos-db) page.

## 3. Update Helm Chart

Once the GitHub release is created, update the Helm Chart for the external scaler in [kedacore/charts](https://github.com/kedacore/charts/tree/master/cosmosdb-scaler) repository. Depending on the changes that went in the release, this might just take updating the `version` and `appVersion` in [Chart.yaml](https://github.com/kedacore/charts/blob/master/cosmosdb-scaler/Chart.yaml) file, or in some cases, might involve adding and updating multiple template files.

Follow the [Contributing](https://github.com/kedacore/charts/blob/master/CONTRIBUTING.md) guide to create a pull request. After the pull request is completed, create a GitHub release in the repository following the same guide.

## 4. Add package version on Artifact Hub

GitHub repository [kedacore/external-scalers](https://github.com/kedacore/external-scalers) showcases the KEDA official external scalers on [Artifact Hub](https://artifacthub.io/packages/search?repo=keda-official-external-scalers) which includes the external scaler for Azure Cosmos DB. For information on why the files in the repository are laid out in a certain way, you can refer to the [KEDA scalers repositories guide](https://artifacthub.io/docs/topics/repositories/#keda-scalers-repositories). The guide also contains link to spec for `artifacthub-pkg.yml` which you may find helpful.

You will need to add a new package version for the external scaler. For this:

1. Copy file `artifacthub-pkg.yml` from directory `artifacthub/azure-cosmos-db/<previous-version>` to `artifacthub/azure-cosmos-db/<new-version>`. Update value of `version` property in the copied file. Make other edits as required.
1. Create a fresh `README.md` inside `artifacthub/azure-cosmos-db/<new-version>` with information on changes introduced by the new release. You can re-use the release description for this one.
1. Create pull request with these changes.
