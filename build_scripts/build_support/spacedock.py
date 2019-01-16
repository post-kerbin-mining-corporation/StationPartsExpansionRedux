# Class for accessing the SpaceDock API
import requests
from contextlib import closing
from requests_toolbelt.multipart.encoder import MultipartEncoder

class SpaceDockAPI(object):

    base_url = "https://spacedock.info"
    login_url = "api/login"
    query_mod_url = "api/mod/{mod_id}"
    update_mod_url = "api/mod/{mod_id}/update"

    def __init__(self, login, password, session=None):
        """
        Initializes the API session

        Inputs:
            login (str): SpaceDock user
            password (str): Spacedock PW
        """
        self.credentials = {"username":login, "password":password}
        self.password = password
        if isinstance(session, requests.Session):
            self.session = session
        else:
            self.session =requests.Session()

    def query_mod(self, mod_id):
        """
        Queries the API for a mod

        Inputs:
            mod_id (str): SpaceDock mod ID

        Returns:
            response (str): The query result
        """
        url = f'{self.base_url}/{self.query_mod_url}'
        print(f"> Getting {url}")
        with closing(self.session.get(url)) as resp:
            try:
                m = getattr(resp, "json")
                return m() if callable(m) else m
            except requests.exceptions.HTTPError as err:
                print(err)
                return resp.text

    def update_mod(self, mod_id, version, changelog, game_version, notify_followers, zip):
        """
        Submits an update to a mod

        Inputs:
            mod_id (str): the Spacedock mod ID
            version (str): the string mod version to use
            changelog (str): a Markdown-formatted changelog
            game_version (str): the string game version to use
            notify_followers (bool): email followers
            zip (str): the path of the zip to upload

        """
        url = f'{self.base_url}/{self.update_mod_url}'.format(mod_id=mod_id)
        payload = {
            "version": version,
            "changelog": changelog,
            "game-version": game_version,
            "notify-followers": "yes" if True else "no"
        }
        print(f"> Posting {payload} to {url}")

        try:
            resp = self.session.post(url, data=payload, files={'zipball': open(zip, 'rb')})
            resp.raise_for_status()
            print(f"{resp.url} returned {resp.text}")
            return resp.text
        except requests.exceptions.HTTPError as err:
            print(f"HTTP ERROR: {err}")
            print(f"{resp.url} returned {resp.text}")
            return resp.text


    def login(self):
        with closing(self.session.post(f'{self.base_url}/{self.login_url}', data=self.credentials)) as resp:
            if resp.reason == 'OK':
                print('"Successfully logged in"')
                return self.session
            else:
                print(resp.text)

    def logout(self):
        pass

    def close(self):
        self.session.close()

    def __call__(self, **kwargs):
        return self.query(**kwargs)

    def __enter__(self):
        self.login()
        return self

    def __exit__(self, *args):
        self.logout()
        self.close()
