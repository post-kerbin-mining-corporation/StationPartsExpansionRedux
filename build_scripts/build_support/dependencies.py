import os
import shutil
import zipfile
import zlib
from build_support.build_helpers import *
import build_support.s3 as s3

DEPENDENCY_BUCKET = "nertea-ksp-modding-dependencies"

def download_dependency(name, info, temp_path, build_path):
    """
    Downloads a dependency record from either S3 (external dependency) or github (internal dependency)

    Inputs:
        name (str): name of the dependency
        info (dict): dictionary describing the dependency
        temp_path (str): path to store dependency zips to
        build_path (str): path to stage to
    """
    if info["location"] == "s3":
        print(f"> Collecting {name} {info['version']} from S3")
        download_dependency_s3(name, info["version"], temp_path, build_path)

    if info["location"] == "github":
        print(f"> Collecting {name} {info['tag']} from repository {info['repository']}")
        download_dependency_github(name, info['repository'], info["tag"], temp_path, build_path)

def download_dependency_s3(name, version, temp_path, build_path):
    """
    Downloads and unzips a dependency from S3

    Inputs:
        name (str): name of the dependency
        version (str): version of the dependency
        temp_path (str): path to store dependency zips to
        build_path (str): path to stage to
    """
    target_name = os.path.join(temp_path, f"{name}_{version}.zip")
    print(f"> Pulling s3://{DEPENDENCY_BUCKET}/external/{name}_{version}.zip")
    s3.copy(f"s3://{DEPENDENCY_BUCKET}/external/{name}_{version}.zip",
        target_name)

    with zipfile.ZipFile(target_name, "r") as z:
        z.extractall(build_path)

def download_dependency_github(name, repo, tag, temp_path, build_path):
    """
    Downloads a github repo with a specific tag

    Inputs:
        name (str): name of the dependency
        repo (str): Account/RepoName
        tag (str): version of the dependency (tagged in the repo)
        temp_path (str): path to store dependency zips to
        build_path (str): path to stage to
    """
    wp = os.getcwd()
    os.chdir(temp_path)
    # Clone into the repo, pull the specified tag
    clone_cmd = f"git clone https://github.com/{repo}.git"
    tag_cmd = f"git checkout master && git fetch && git fetch --tags && git checkout {tag}"
    os.system(clone_cmd)
    os.chdir(name)
    os.system(tag_cmd)
    os.chdir(wp)
    # Move the contents of GameData into the build directory
    shutil.copytree(os.path.join(temp_path, name, "GameData", name), os.path.join(build_path, "GameData", name))
