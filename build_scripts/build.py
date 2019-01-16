# Build script entrypoint
import os
import shutil
import zipfile
import zlib
import sys
from argparse import ArgumentParser

from build_support.build_helpers import *
from build_support.dependencies import download_dependency


# Build paths
TEMP_PATH = "tmp"
BUILD_PATH = "build"
DEPLOY_PATH = "deploy"

# Root directoy files to keep in the archives
VALID_FILES = ["changelog.txt", "readme.txt"]

def build_nodep_release(version_data, mod_name):
    """
    Builds the release zip with no included dependencies

    Inputs:
        version_data (dict): Contents of the .version file
        mod_name (str): name of the mod
    """
    build_path = os.path.join(DEPLOY_PATH,
        f"{mod_name}_Core_" + "{MAJOR}_{MINOR}_{PATCH}".format(**version_data["VERSION"]))
    shutil.make_archive(build_path, 'zip', os.path.join(BUILD_PATH))
    print(f"> Built {build_path}")

def build_full_release(version_data, mod_name):
    """
    Builds the release zip with a full set of required dependencies

    Inputs:
        version_data (dict): Contents of the .version file
        mod_name (str): name of the mod
    """
    build_path = os.path.join(DEPLOY_PATH,
        f"{mod_name}_" + "{MAJOR}_{MINOR}_{PATCH}".format(**version_data["VERSION"]))
    shutil.make_archive(build_path, 'zip', os.path.join(BUILD_PATH))
    print(f"> Built {build_path}")

def build_extras(version_data, build_packages=False):
    """
    Compiles and optionally builds packages for all Extras in the mod

    Inputs:
        version_data (dict): Contents of the .version file
        build_packages (bool): whether to create an individual zipfile for each package
    """
    for root, dirs, files in os.walk("Extras"):
        for name in dirs:
            build_extra(name, version_data, build_packages)

def build_extra(name, version_data, build_package):
    """
    Compiles and optionally builds a single Extras package

    Inputs:
        name (str): name of the extra
        version_data (dict): Contents of the .version file
        build_package (bool): whether to create an individual zipfile for the package
    """
    extra_path = os.path.join(DEPLOY_PATH, f"{name}" + "{MAJOR}_{MINOR}_{PATCH}".format(**version_data["VERSION"]))
    print(f"> Compiling Extra {name}")
    ensure_path(os.path.join(BUILD_PATH,"Extras"))
    shutil.copytree(os.path.join("Extras", name), os.path.join(BUILD_PATH,"Extras", name))

    if build_package:
        print(f"> Building {name}")
        shutil.make_archive(extra_path, "zip", os.path.join(BUILD_PATH, "Extras", name))
        print(f"> Built {extra_path}")

def collect_dependencies(dep_data):
    """
    Finds and downloads all the mod's dependencies

    Inputs:
        dep_data (dict): dictionart of dependecies from build_data.json
    """
    clean_path(TEMP_PATH)
    for name, info in dep_data.items():
        download_dependency(name, info, TEMP_PATH, BUILD_PATH)
    cleanup()

def cleanup():
    """
    Cleans up the trailing files in the main directory for packaging by excluding all expect the
    specified items in VALID_FILES
    """
    onlyfiles = [f for f in os.listdir(BUILD_PATH) if os.path.isfile(os.path.join(BUILD_PATH, f))]
    for f in onlyfiles:
        if f not in VALID_FILES:
            os.remove(os.path.join(BUILD_PATH,f))

def bundle(core_release, extras_release, complete_release):
    """
    Compiles and builds the set of release packages according to information from
    the .version file and the build_data.json file
    """
    # Collect build information
    build_data = get_build_data()
    version_data = get_version_file_info(build_data["mod_name"])

    print(f"Building {build_data['mod_name']} version {get_version(version_data)}\n=================")

    # Clean/recreate the build, deploy and temp paths
    clean_path(os.path.join(BUILD_PATH))
    clean_path(os.path.join(DEPLOY_PATH))
    clean_path(os.path.join(TEMP_PATH))

    # Copy main mod content
    print(f"Compiling core mod content")
    shutil.copytree(os.path.join("GameData", build_data["mod_name"]), os.path.join(BUILD_PATH, "GameData", build_data["mod_name"]))
    shutil.copy("changelog.txt", os.path.join(BUILD_PATH, "changelog.txt"))
    shutil.copy("readme.txt", os.path.join(BUILD_PATH,  "readme.txt"))

    if core_release:
        print(f"Building BASIC release package")
        build_nodep_release(version_data, build_data['mod_name'])

    if os.path.exists("Extras"):
      print(f"Compiling and building EXTRAS release packages")
      build_extras(version_data, extras_release)

    print(f"Compiling complete release package")
    print(f"> Collecting dependencies")
    collect_dependencies(build_data["dependencies"])

    if complete_release:
        print(f"Building COMPLETE release package")
        build_full_release(version_data, build_data['mod_name'])

    # Write the version/changelog out in text for Travis deploy scripts to take advantage of as env variables set are not persisted
    with open(os.path.join("build_scripts", 'version.txt'), "w") as f:
      f.write(get_version(version_data))

    with open(os.path.join("build_scripts", 'changelog.md'), "w") as f:
      f.write(get_changelog())

if __name__ == "__main__":
    parser = ArgumentParser()
    parser.add_argument("-c", "--complete",
                        action="store_true",  default=True,
                        help="write complete package")
    parser.add_argument("-e", "--extras",
                        action="store_true", default=False,
                        help="write extras package")
    parser.add_argument("-b", "--basic",
                        action="store_true", default=False,
                        help="write basic no dependency package")

    args = parser.parse_args()

    bundle(args.basic, args.extras, args.complete)
