# Useful functions for build scripts
import os
import json
import stat

BUILD_SCRIPT_PATH = "build_scripts"
BUILD_DATA_NAME = "build_data.json"
CHANGELOG_PATH = "changelog.txt"

class tcolors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

def get_version(version_data):
  """Returns a formatted version string from the version data dictionary"""
  return "{MAJOR}.{MINOR}.{PATCH}".format(**version_data["VERSION"])

def get_ksp_version(version_data):
  """Returns a formatted KSP version string from the version data dictionary"""
  return "{MAJOR}.{MINOR}.{PATCH}".format(**version_data["KSP_VERSION"])

def get_version_file_info(mod_name):
  """Extracts version info from the .version file"""
  version_path = os.path.join("GameData", mod_name, "Versioning", f"{mod_name}.version")
  with open(version_path, "r") as f:
    version_data = json.load(f)
  return version_data

def get_build_data():
    """Loads the information from the build data file"""
    build_data_path = os.path.join(BUILD_SCRIPT_PATH, BUILD_DATA_NAME)
    with open(build_data_path, "r") as f:
      build_data = json.load(f)
    return build_data

def ensure_path(path):
    """Ensure a path exists, make it if not"""
    if os.path.exists(path):
        return
    else:
        os.makedirs(path)

def clean_path(path):
    """Creates a clean copy of a path if it exists"""
    if os.path.exists(path):
        for root, dirs, files in os.walk(path, topdown=False):
            for name in files:
                filename = os.path.join(root, name)
                os.chmod(filename, stat.S_IWUSR)
                os.remove(filename)
            for name in dirs:
                os.rmdir(os.path.join(root, name))
    else:
        os.makedirs(path)

def get_changelog():
  """Extracts a markdown formatted version of the latest changelog.txt entry"""
  log_lines = []
  with open(CHANGELOG_PATH, "r") as f:
    for idx, line in enumerate(f):
        if line.startswith("---") or line.startswith("v"):
            pass
        else:
            log_lines.append(line.replace("- ", "* "))
        if idx > 1 and line == "\n":
            break
  return "".join(log_lines)
