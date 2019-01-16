# Useful functions for working with Amazon S3
import boto3
import botocore
from urllib.parse import urlparse

def copy(src, dest):
    """
    Copies a file to or from s3. If a link starts with s3: it will be treated
    as an S3 path. Uses local AWS keys to do this

    Inputs:
        src (str): url of source file
        dest (str): url of destination file
    """
    s3 = boto3.client('s3', verify=False)
    if src.startswith("s3"):
        url_parsed = urlparse(src)
        s3.download_file(url_parsed.netloc, url_parsed.path[1:], dest)
    if dest.startswith("s3"):
        url_parsed = urlparse(dest)
        s3.upload_file(src, url_parsed.netloc, url_parsed.path)
