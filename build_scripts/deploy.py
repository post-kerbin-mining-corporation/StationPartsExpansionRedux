# Deploy script entrypoint
import os
import shutil
import zipfile
import zlib
from argparse import ArgumentParser

from build_support.build_helpers import *
from build_support.spacedock import SpaceDockAPI
from build_support.curseforge import CurseForgeAPI


def deploy(spacedock, curse):
    """
    Deploys packages to providers

    Inputs:
        spacedock (bool): deploy to SpaceDock
        curse (bool): deploy to CurseForge
    """
    build_data = get_build_data()
    version_data = get_version_file_info(build_data["mod_name"])
    changelog = get_changelog()

    print(f"Deploying {build_data['mod_name']} version {get_version(version_data)}\n=================")
    print(f"Changes:\n{changelog}")

    print(f"Targets: {'SpaceDock ' if spacedock else ''}{'Curse' if curse else ''}")
    zipfile = os.path.join("deploy", f"{build_data['mod_name']}_" + "{MAJOR}_{MINOR}_{PATCH}.zip".format(**version_data["VERSION"]))
    print(f"> Deploying file {zipfile}")
    if spacedock:
        deploy_spacedock(
            get_version(version_data),
            get_ksp_version(version_data),
            build_data['spacedock']['mod-id'],
            changelog,
            zipfile)
    if curse:
        deploy_curseforge(
            get_ksp_version(version_data),
            build_data['curseforge']['mod-id'],
            changelog,
            zipfile)

def deploy_curseforge(ksp_version, mod_id, changelog, zipfile):
    """
    Performs deployment to CurseForge

    Inputs:
        ksp_version (str): version of KSP to target
        mod_id (str): CurseForge project ID
        changelog (str): Markdown formatted changelog
        zipfile (str): path to file to upload
    """
    print("> Deploying to CurseForge")
    with CurseForgeAPI(os.environ["CURSEFORGE_TOKEN"]) as api:
        api.update_mod(mod_id, changelog, ksp_version, "release", zipfile)

def deploy_spacedock(version, ksp_version, mod_id, changelog, zipfile):
    """
    Performs deployment to SpaceDock

    Inputs:
        version (str): mod version
        ksp_version (str): version of KSP to target
        mod_id (str): CurseForge project ID
        changelog (str): Markdown formatted changelog
        zipfile (str): path to file to upload
    """
    print("> Deploying to SpaceDock")
    with SpaceDockAPI(os.environ["SPACEDOCK_LOGIN"], os.environ["SPACEDOCK_PASSWORD"]) as api:
        api.update_mod(mod_id, version, changelog, ksp_version, True, zipfile)

if __name__ == "__main__":
    parser = ArgumentParser()
    parser.add_argument("-c", "--curse",
                        action="store_true",  default=False,
                        help="deploy to curse")
    parser.add_argument("-s", "--spacedock",
                        action="store_true", default=False,
                        help="deploy to spacedock")

    args = parser.parse_args()
    deploy(args.spacedock, args.curse)
