#!/usr/bin/env python3
import json
import os
import sys
import time
import urllib.error
import urllib.request


BASE_URL = os.environ.get("API_BASE_URL", "http://localhost:8080").rstrip("/")
PASSWORD = "SmokePass123!"


def request(method, path, body=None, token=None, expected=200):
    data = None
    headers = {"Accept": "application/json"}

    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    if token is not None:
        headers["Authorization"] = f"Bearer {token}"

    req = urllib.request.Request(
        f"{BASE_URL}{path}",
        data=data,
        headers=headers,
        method=method,
    )

    try:
        with urllib.request.urlopen(req, timeout=15) as response:
            response_body = response.read().decode("utf-8")
            status = response.status
    except urllib.error.HTTPError as error:
        response_body = error.read().decode("utf-8")
        status = error.code

    if status != expected:
        raise AssertionError(
            f"{method} {path} expected {expected}, got {status}: {response_body}"
        )

    if not response_body:
        return None

    return json.loads(response_body)


def assert_not_contains_key(value, forbidden):
    if isinstance(value, dict):
        for key, child in value.items():
            lowered = key.lower()
            for forbidden_key in forbidden:
                if forbidden_key in lowered:
                    raise AssertionError(f"Public response contains forbidden key: {key}")
            assert_not_contains_key(child, forbidden)
        return

    if isinstance(value, list):
        for child in value:
            assert_not_contains_key(child, forbidden)


def create_initiative(token, title, interest_id, status=None, visibility=None):
    body = {
        "title": title,
        "shortDescription": f"Smoke test initiative {title}",
        "goalType": 3,
        "interestIds": [interest_id],
        "roles": ["Participant"],
        "university": "Smoke University",
        "teamSize": 2,
    }

    if status is not None:
        body["status"] = status

    if visibility is not None:
        body["visibility"] = visibility

    return request("POST", "/api/initiatives", body, token=token)


def main():
    suffix = f"{int(time.time())}-{os.getpid()}"
    email = f"public-initiative-smoke-{suffix}@example.com"

    auth = request(
        "POST",
        "/api/auth/register",
        {
            "email": email,
            "displayName": "Public Initiative Smoke",
            "password": PASSWORD,
        },
    )
    token = auth["token"]

    interest = request(
        "POST",
        "/api/interests",
        {
            "name": f"Smoke Public Sharing {suffix}",
            "slug": f"smoke-public-sharing-{suffix}",
        },
        token=token,
    )
    interest_id = interest["id"]

    public = create_initiative(
        token,
        f"Looking for hackathon teammates {suffix}",
        interest_id,
    )
    private = create_initiative(
        token,
        f"Private walk in Kyiv {suffix}",
        interest_id,
        visibility=2,
    )
    archived = create_initiative(
        token,
        f"Archived Minecraft night {suffix}",
        interest_id,
        status=4,
    )
    duplicate_one = create_initiative(
        token,
        f"Duplicate slug title {suffix}",
        interest_id,
    )
    duplicate_two = create_initiative(
        token,
        f"Duplicate slug title {suffix}",
        interest_id,
    )

    if not public.get("slug"):
        raise AssertionError("Created public initiative did not receive a slug")

    if duplicate_one["slug"] == duplicate_two["slug"]:
        raise AssertionError("Duplicate titles produced the same slug")

    public_response = request(
        "GET",
        f"/api/public/initiatives/{public['slug']}",
        expected=200,
    )

    if public_response["slug"] != public["slug"]:
        raise AssertionError("Public endpoint returned the wrong initiative")

    request(
        "GET",
        f"/api/public/initiatives/{private['slug']}",
        expected=404,
    )
    request(
        "GET",
        f"/api/public/initiatives/{archived['slug']}",
        expected=404,
    )

    assert_not_contains_key(public_response, ["joinrequests", "join_requests"])
    assert_not_contains_key(
        public_response,
        ["email", "phone", "password", "token", "refresh", "owneruserid"],
    )

    print("PASS public initiative smoke tests")


if __name__ == "__main__":
    try:
        main()
    except Exception as exception:
        print(f"FAIL public initiative smoke tests: {exception}", file=sys.stderr)
        sys.exit(1)
